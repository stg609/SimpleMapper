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
    public class Mapper
    {
        ConcurrentDictionary<Tuple<Type, Type>, List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>>> _maps = new ConcurrentDictionary<Tuple<Type, Type>, List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>>>();
        ConcurrentDictionary<string, Func<object, object, object, object, object>> _mapMethodDelegates = new ConcurrentDictionary<string, Func<object, object, object, object, object>>();
        ConcurrentDictionary<string, Action<object, object>> _setterMethodDelegates = new ConcurrentDictionary<string, Action<object, object>>();
        ConcurrentDictionary<string, Func<object, object>> _getterMethodDelegates = new ConcurrentDictionary<string, Func<object, object>>();
        ConcurrentDictionary<string, Func<object>> _ctorMethodDelegates = new ConcurrentDictionary<string, Func<object>>();
        ConcurrentDictionary<string, Type> _genericTypes = new ConcurrentDictionary<string, Type>();
        ConcurrentDictionary<string, MethodInfo> _methodInfoCache = new ConcurrentDictionary<string, MethodInfo>();
        static readonly Type _objType = typeof(object);
        static readonly Type _listType = typeof(List<>);
        static readonly Type _actObjType = typeof(Action<object, object>);
        static readonly Type _funcObjType = typeof(Func<object, object>);
        static readonly Type _mapDelegateType = typeof(Func<object, object, object, object, object>);
        static readonly MethodInfo _mapBaseMethodInfo = typeof(Mapper).GetMethod("Map");

        /// <summary>
        /// To Add a mapping rule between two property 
        /// </summary>
        /// <typeparam name="TSource">Source Type</typeparam>
        /// <typeparam name="TTarget">Target Type</typeparam>
        /// <param name="source">Expression of the source property</param>
        /// <param name="target">Expression of the target property</param>
        /// <param name="useDefaultValue">Whether to use default value of the property. Default value will be used when the predication is true</param>
        /// <returns>Mapper</returns>
        public Mapper AddMap<TSource, TTarget>(Expression<Func<TSource, object>> source, Expression<Func<TTarget, object>> target, Predicate<TSource> useDefaultValue = null)
            where TTarget : class, new()
            where TSource : class
        {
            ParameterExpression[] parameters = target.Parameters.ToArray();
            List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>> delegates = null;
            _maps.TryGetValue(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), out delegates);
            if (delegates == null)
            {
                delegates = new List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>>();
                _maps.AddOrUpdate(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), delegates, (key, val) => delegates);
            }
            try
            {
                Expression targetBody = target.Body;
                Expression sourceBody = source.Body;
                if (targetBody.NodeType == ExpressionType.Convert)
                {
                    targetBody = ((UnaryExpression)targetBody).Operand;
                }
                if (sourceBody.NodeType == ExpressionType.Convert)
                {
                    sourceBody = ((UnaryExpression)sourceBody).Operand;
                }
                //{ TRequest.prop = TViewModel.prop}
                BlockExpression block = Expression.Block(Expression.Assign(targetBody, sourceBody));
                // (TRequest, TViewModel)=>{ TRequest.prop = TViewModel.prop}
                LambdaExpression assignExpression = Expression.Lambda<Action<TSource, TTarget>>(block, source.Parameters.Concat(parameters));


                //Tuple - bool - indicate whether the source and target expression is a compatible expression
                //Tuple - Delegate 1 - indicate the predication about whether to use default value
                //Tuple - Delegate 2 - indicate the compatible delegate or source delegate of incompatible delegate
                delegates.Add(new Tuple<bool, Delegate, Delegate, Delegate, Type, Type>(true, useDefaultValue, assignExpression.Compile(), null, null, null));
            }
            catch (Exception)
            {
                //Tuple - bool - indicate whether the source and target expression is a compatible expression
                //Tuple - Delegate 1 - indicate the predication about whether to use default value
                //Tuple - Delegate 2 - indicate the target property 
                //Tuple - Delegate 3 - indicate the source property
                //Tuple - Type 1 - indicate the target type
                //Tuple - Type 2 - indicate the source type
                delegates.Add(new Tuple<bool, Delegate, Delegate, Delegate, Type, Type>(false, useDefaultValue, target.Compile(), source.Compile(), target.Body.Type, source.Body.Type));
            }

            return this;
        }

        /// <summary>
        /// To Add a custom mapping rules for two objects
        /// </summary>
        /// <typeparam name="TSource">Source Type</typeparam>
        /// <typeparam name="TTarget">Target Type</typeparam>
        /// <param name="mapping">Mapping Rule</param>
        /// <returns>Mapper</returns>
        public Mapper AddMap<TSource, TTarget>(Action<TSource, TTarget> mapping)
            where TTarget : class, new()
            where TSource : class
        {
            List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>> delegates = null;
            _maps.TryGetValue(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), out delegates);
            if (delegates == null)
            {
                delegates = new List<Tuple<bool, Delegate, Delegate, Delegate, Type, Type>>();
                _maps.AddOrUpdate(new Tuple<Type, Type>(typeof(TTarget), typeof(TSource)), delegates, (key, val) => delegates);
            }

            delegates.Add(new Tuple<bool, Delegate, Delegate, Delegate, Type, Type>(true, new Predicate<TSource>(o => false), mapping, null, null, null));
            return this;
        }

        public void ClearMaps()
        {
            _maps.Clear();
            _mapMethodDelegates.Clear();
            _setterMethodDelegates.Clear();
            _getterMethodDelegates.Clear();
            _ctorMethodDelegates.Clear();
            _genericTypes.Clear();
            _methodInfoCache.Clear();
        }

        /// <summary>
        /// Map from source Type to target Type
        /// </summary>
        /// <typeparam name="TSource">Source Type</typeparam>
        /// <typeparam name="TTarget">Target Type</typeparam>
        /// <param name="source">source object</param>
        /// <param name="target">target object</param>
        /// <param name="container">Unity Container which containing the logic to instantiate the target type</param>
        /// <returns>Target object after mapping</returns>
        public TTarget Map<TSource, TTarget>(TSource source, TTarget target = null, UnityContainer container = null)
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

            MethodInfo mapMethodInfo = null;
            MethodInfo getterMethodInfo = null;
            MethodInfo setterMethodInfo = null;
            Action<object, object> setterAction = null;
            Func<object, object> getterFunc = null;
            Func<object, object, object, object, object> mapFunc = null;

            //If this is complex type, then recursion
            foreach (PropertyInfo property in targetType.GetProperties())
            {
                if (property.PropertyType.IsSimpleType())
                {
                    continue;
                }

                if (!property.PropertyType.IsEnumerable())
                {
                    List<Type> sourceTypeMapToTargetType = (from map in _maps
                                                            where map.Key.Item1 == property.PropertyType
                                                            select map.Key.Item2).ToList();

                    if (sourceTypeMapToTargetType.Count == 0 && container != null && container.IsRegistered<IObjectFactory>(property.PropertyType.FullName))
                    {
                        //if the target type is not existed in the mapping rule and also registed in the Unity which means the target should be instantiated whatever.
                        sourceTypeMapToTargetType.Add(sourceType);
                    }

                    foreach (Type sType in sourceTypeMapToTargetType)
                    {
                        if (sType == sourceType)
                        {
                            mapMethodInfo = GetMapGenericMethod(sourceType, property.PropertyType);
                            mapFunc = GetInvoker(_mapMethodDelegates, _mapDelegateType, mapMethodInfo);

                            getterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Get);// property.GetGetMethod();
                            getterFunc = GetInvoker(_getterMethodDelegates, _funcObjType, getterMethodInfo);

                            object propValue = mapFunc(this, source, getterFunc(result), container);
                            setterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Set); // property.GetSetMethod();
                            setterAction = GetInvoker(_setterMethodDelegates, _actObjType, setterMethodInfo);

                            setterAction(result, propValue);
                        }
                        else
                        {
                            PropertyInfo sourceProp = sourceType.GetProperties().SingleOrDefault(prop => prop.PropertyType == sType);

                            mapMethodInfo = GetMapGenericMethod(sType, property.PropertyType);
                            mapFunc = GetInvoker(_mapMethodDelegates, _mapDelegateType, mapMethodInfo);

                            getterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Get);
                            getterFunc = GetInvoker(_getterMethodDelegates, _funcObjType, getterMethodInfo);

                            MethodInfo getterSourceMethodInfo = GetPropertyMethodInfo(sourceProp, PropertyType.Get);
                            Func<object, object> getterSourceFunc = GetInvoker(_getterMethodDelegates, _funcObjType, getterSourceMethodInfo);

                            object propValue = mapFunc(this, getterSourceFunc(source), getterFunc(result), container);
                            setterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Set);
                            setterAction = GetInvoker(_setterMethodDelegates, _actObjType, setterMethodInfo);

                            setterAction(result, propValue);
                        }
                    }
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
                    Action<TSource, TTarget> mapAction = delg.Item3 as Action<TSource, TTarget>;
                    Predicate<TSource> predicate = delg.Item2 as Predicate<TSource>;
                    if (mapAction != null && (predicate == null || !predicate(source)))
                    {
                        mapAction(source, result);
                    }
                }
                else
                {
                    targetDelegate = delg.Item3 as Func<TTarget, object>;
                    sourceDelegate = delg.Item4 as Func<TSource, object>;

                    object sourceProp = sourceDelegate(source);
                    if (sourceProp == null)
                    {
                        throw new ArgumentNullException("The property of the source object can't be null");
                    }
                    else if (sourceProp.GetType().IsArray)
                    {
                        MapArray(result, targetType, delg.Item5, sourceProp, delg.Item6.GetElementType(), _mapDelegateType,
                             _mapMethodDelegates, _setterMethodDelegates, _ctorMethodDelegates, container);
                    }
                    else if (sourceProp.GetType().IsEnumerable())
                    {
                        throw new NotSupportedException(String.Format("{0} is not supported", sourceProp.GetType()));
                    }
                }
            });

            return result;
        }

        /// <summary>
        /// Automatically map between two objects according to the property name
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TTarget"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public TTarget AutoMap<TSource, TTarget>(TSource source, TTarget target = null)
            where TTarget : class, new()
            where TSource : class
        {
            Type targetType = typeof(TTarget);
            Type sourceType = typeof(TSource);

            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            if (target == null)
            {
                target = new TTarget();
            }

            MethodInfo getterMethodInfo = null;
            MethodInfo setterMethodInfo = null;
            Func<object, object> getterFunc = null;
            Action<object, object> setterAction = null;
            IEnumerable<PropertyInfo> sourceProps = sourceType.GetProperties();
            foreach (PropertyInfo property in targetType.GetProperties())
            {
                PropertyInfo mappedProp = sourceProps.SingleOrDefault(prop => prop.Name.Equals(property.Name) && prop.PropertyType == property.PropertyType);

                if (mappedProp != null && property.PropertyType.IsSimpleType())
                {
                    getterMethodInfo = GetPropertyMethodInfo(mappedProp, PropertyType.Get);// property.GetGetMethod();
                    getterFunc = GetInvoker(_getterMethodDelegates, _funcObjType, getterMethodInfo);

                    setterMethodInfo = GetPropertyMethodInfo(property, PropertyType.Set); // property.GetSetMethod();
                    setterAction = GetInvoker(_setterMethodDelegates, _actObjType, setterMethodInfo);
                    setterAction(target, getterFunc(source));
                }
            }

            return target;
        }

        private void MapArray(object target, Type targetType, Type targetArrayPropType, object sourceProp,
            Type sourceArrayPropItemType, Type mapDelegateType,
            ConcurrentDictionary<string, Func<object, object, object, object, object>> mapDelegateCache,
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
                Func<object, object, object, object, object> mapFunc = GetInvoker(mapDelegateCache, mapDelegateType, mapMethodInfo);

                tempListInstance.Add(mapFunc(this, vmItm, null, container));
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

        MethodInfo GetPropertyMethodInfo(PropertyInfo prop, PropertyType propType)
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

        MethodInfo GetMapGenericMethod(Type sourceType, Type targetType)
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

    internal enum PropertyType
    {
        Get,
        Set
    }
}
