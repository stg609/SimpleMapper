using Microsoft.Practices.Unity;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMapper
{

    public static class Mapper
    {
        static ConcurrentDictionary<Tuple<Type, Type>, List<Tuple<bool, Delegate, Delegate, Type, Type>>> _maps = new ConcurrentDictionary<Tuple<Type, Type>, List<Tuple<bool, Delegate, Delegate, Type, Type>>>();
        static ConcurrentDictionary<string, Func<object, object, object, object>> _mapMethodDelegates = new ConcurrentDictionary<string, Func<object, object, object, object>>();
        static ConcurrentDictionary<string, Action<object, object>> _setterMethodDelegates = new ConcurrentDictionary<string, Action<object, object>>();
        static ConcurrentDictionary<string, Func<object, object>> _getterMethodDelegates = new ConcurrentDictionary<string, Func<object, object>>();
        static ConcurrentDictionary<string, Func<object>> _ctorMethodDelegates = new ConcurrentDictionary<string, Func<object>>();
        static ConcurrentDictionary<string, Type> _genericTypes = new ConcurrentDictionary<string, Type>();
        static ConcurrentDictionary<string, MethodInfo> _methodInfoCache = new ConcurrentDictionary<string, MethodInfo>();
        static readonly Type _objType = typeof(object);
        static readonly Type _listType = typeof(List<>);
        static readonly Type _actObjType = typeof(Action<object, object>);
        static readonly Type _funcObjType = typeof(Func<object, object>);
        static readonly Type _mapDelegateType = typeof(Func<object, object, object, object>);
        static readonly MethodInfo _mapBaseMethodInfo = typeof(Mapper).GetMethod("Map");

        public static void AddMap<TSource, TTarget>(Expression<Func<TSource, object>> source, Expression<Func<TTarget, object>> target)
            where TTarget : class, new()
            where TSource : class
        {
            ParameterExpression[] parameters = target.Parameters.ToArray();
            List<Tuple<bool, Delegate, Delegate, Type, Type>> delegates = null;
            _maps.TryGetValue(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), out delegates);
            if (delegates == null)
            {
                delegates = new List<Tuple<bool, Delegate, Delegate, Type, Type>>();
                _maps.AddOrUpdate(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), delegates, (key, val) => delegates);
            }
            try
            {
                //{ TRequest.prop = TViewModel.prop}
                BlockExpression block = Expression.Block(Expression.Assign(target.Body, source.Body));
                // (TRequest, TViewModel)=>{ TRequest.prop = TViewModel.prop}
                LambdaExpression assignExpression = Expression.Lambda<Action<TTarget, TSource>>(block, parameters.Concat(source.Parameters));

                delegates.Add(new Tuple<bool, Delegate, Delegate, Type, Type>(true, assignExpression.Compile(), null, null, null));
            }
            catch (Exception)
            {
                delegates.Add(new Tuple<bool, Delegate, Delegate, Type, Type>(false, target.Compile(), source.Compile(), target.Body.Type, source.Body.Type));
            }
        }

        public static TTarget Map<TSource, TTarget>(TSource source, TTarget target, UnityContainer container = null)
            where TTarget : class, new()
            where TSource : class
        {
            Type targetType = typeof(TTarget);
            Type sourceType = typeof(TSource);
            TTarget result = null;

            if (target == null)
            {
                //init
                if (container == null || !container.IsRegistered<IObjectFactory>(typeof(TTarget).FullName))
                {
                    result = new TTarget();
                }
                else
                {
                    result = container.Resolve<IObjectFactory>(typeof(TTarget).FullName).InitializeType() as TTarget;
                }
            }
            else
            {
                result = target;
            }

            //If there're complex type, then recursion
            foreach (PropertyInfo property in targetType.GetProperties())
            {
                if (property.PropertyType.IsSimpleType())
                {
                    continue;
                }

                if (!property.PropertyType.IsEnumerable())
                {
                    MethodInfo mapMethodInfo = GetMapGenericMethod(sourceType, property.PropertyType);
                    Func<object, object, object, object> mapFunc = GetInvoker(_mapMethodDelegates, _mapDelegateType, mapMethodInfo);


                    MethodInfo getterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Get);// property.GetGetMethod();
                    Func<object, object> getterFunc = GetInvoker(_getterMethodDelegates, _funcObjType, getterMethodInfo);

                    object propValue = mapFunc(source, getterFunc(result), container);
                    MethodInfo setterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Set); // property.GetSetMethod();
                    Action<object, object> setterAction = GetInvoker(_setterMethodDelegates, _actObjType, setterMethodInfo);

                    setterAction(result, propValue);
                }
            }

            //Map all other properties
            var delegates = _maps.SingleOrDefault(itm => itm.Key.Item1 == targetType && itm.Key.Item2 == sourceType);
            if (delegates.Value == null)
            {
                return result;
            }

            Func<TTarget, object> targetDelegate = null;
            Func<TSource, object> sourceDelegate = null;
            delegates.Value.ForEach(delg =>
            {
                if (delg.Item1)
                {
                    Action<TTarget, TSource> mapAction = delg.Item2 as Action<TTarget, TSource>;
                    if (mapAction != null)
                    {
                        mapAction(result, source);
                    }
                }
                else
                {
                    targetDelegate = delg.Item2 as Func<TTarget, object>;
                    sourceDelegate = delg.Item3 as Func<TSource, object>;

                    object sourceProp = sourceDelegate(source);
                    if (sourceProp != null && sourceProp.GetType().IsArray)
                    {
                        MapArray(result, targetType, delg.Item4, sourceProp, delg.Item5.GetElementType(), _mapDelegateType,
                             _mapMethodDelegates, _setterMethodDelegates, _ctorMethodDelegates, container);

                    }
                }
            });

            return result;
        }
        
        static void MapArray(object target, Type targetType, Type targetArrayPropType, object sourceProp,
            Type sourceArrayPropItemType, Type mapDelegateType,
            ConcurrentDictionary<string, Func<object, object, object, object>> mapDelegateCache,
            ConcurrentDictionary<string, Action<object, object>> setterDelegateCache,
            ConcurrentDictionary<string, Func<object>> ctorMethodDelegates,
            UnityContainer container = null)
        {
            Type targetArrayPropItemType = targetArrayPropType.GetElementType();
            IEnumerator sourceArrEnumerator = ((IEnumerable)sourceProp).GetEnumerator();

            Type tempList = null;
            string genericTypeIdentity = _listType.ToString() + targetArrayPropItemType.ToString();
            _genericTypes.TryGetValue(genericTypeIdentity, out tempList);

            if (tempList == null)
            {
                tempList = _listType.MakeGenericType(targetArrayPropItemType);
                _genericTypes.AddOrUpdate(genericTypeIdentity, tempList, (key, val) => tempList);
            }   
            ConstructorInfo ctorInfo = tempList.GetConstructor(Type.EmptyTypes);

            int len = 0;
            Func<object> ctorFunc = GetConstuctor(ctorMethodDelegates, ctorInfo.DeclaringType.ToString() + ".ctor", ctorInfo);

            IList tempListInstance = ctorFunc() as IList;

            while (sourceArrEnumerator.MoveNext())
            {
                object vmItm = sourceArrEnumerator.Current;
                if (vmItm == null)
                {
                    continue;
                }

                MethodInfo mapMethodInfo = GetMapGenericMethod(sourceArrayPropItemType, targetArrayPropItemType);
                Func<object, object, object, object> mapFunc = GetInvoker(mapDelegateCache, mapDelegateType, mapMethodInfo);

                tempListInstance.Add(mapFunc(vmItm, null, container));
                len++;
            }

            MethodInfo setterMethodInfo = GetPropertyMethodInfo(targetType.GetProperties().Single(prop => prop.PropertyType == targetArrayPropType), PropertyType.Set);// targetType.GetProperties().Single(prop => prop.PropertyType == targetArrayPropType).GetSetMethod();
            Action<object, object> setterAction = GetInvoker(setterDelegateCache, _actObjType, setterMethodInfo);

            //List to Array
            Array arr = Array.CreateInstance(targetArrayPropItemType, len);
            for (int index = 0; index < len; index++)
            {
                arr.SetValue(tempListInstance[index], index);
            }

            setterAction(target, arr);
        }

        static MethodInfo GetPropertyMethodInfo(PropertyInfo prop, PropertyType propType)
        {
            MethodInfo methodInfo = null;
            string key = prop.PropertyType.ToString() + propType.ToString();
            _methodInfoCache.TryGetValue(key, out methodInfo);
            if (methodInfo == null)
            {
                switch (propType)
                {
                    case PropertyType.Get:
                        methodInfo = prop.GetMethod;
                        break;
                    case PropertyType.Set:
                        methodInfo = prop.SetMethod;
                        break;
                }

                _methodInfoCache.AddOrUpdate(key, methodInfo, (k, v) => methodInfo);
            }

            return methodInfo;
        }

        static MethodInfo GetMapGenericMethod(Type sourceType, Type targetType)
        {
            MethodInfo mapMethodInfo = null;
            string key = sourceType.ToString() + targetType.ToString();
            _methodInfoCache.TryGetValue(key, out mapMethodInfo);
            if (mapMethodInfo == null)
            {
                mapMethodInfo = _mapBaseMethodInfo.MakeGenericMethod(new Type[] { sourceType, targetType });
                _methodInfoCache.AddOrUpdate(key, mapMethodInfo, (k, v) => mapMethodInfo);
            }

            return mapMethodInfo;
        }

        static T GetInvoker<T>(ConcurrentDictionary<string, T> cache, Type invokerDelegateType, MethodInfo methodInfo)
            where T : class
        {
            T invoker = null;

            cache.TryGetValue(methodInfo.ToString(), out invoker);
            if (invoker == null)
            {
                invoker = FastInvoker.GetInvoker(invokerDelegateType, methodInfo) as T;
                cache.AddOrUpdate(methodInfo.ToString(), invoker, (key, val) => invoker);
            }

            return invoker;
        }

        static Func<object> GetConstuctor(ConcurrentDictionary<string, Func<object>> cache, string ctorIdentity, ConstructorInfo ctorInfo)
        {
            Func<object> ctor = null;
            cache.TryGetValue(ctorIdentity, out ctor);

            if (ctor == null)
            {
                ctor = FastInvoker.GetConstructor(ctorInfo);
                cache.AddOrUpdate(ctorIdentity, ctor, (key, val) => ctor);
            }

            return ctor;
        }
    }

    enum PropertyType
    {
        Get,
        Set
    }
}
