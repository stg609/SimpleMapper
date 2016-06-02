using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace SimpleMapper
{
    public static class FastInvoker
    {
        public static Delegate GetInvoker(Type delegateType, MethodInfo method)
        {
            List<Type> parameterTypes = new List<Type>();
            ParameterInfo[] paramInfos = method.GetParameters();

            if (!method.IsStatic)
            {
                parameterTypes.Add(typeof(object));
            }

            foreach (ParameterInfo parameter in paramInfos)
            {
                parameterTypes.Add(typeof(object));
            }

            //Dynamic set TRequest.prop = propValue
            DynamicMethod dym = new DynamicMethod(string.Empty,
                method.ReturnType, parameterTypes.ToArray(),
                method.DeclaringType.Module);

            ILGenerator il = dym.GetILGenerator();
            int index = 0;
            if (!method.IsStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                index++;
            }
            for (int i = index; i < paramInfos.Length + index; i++)
            {
                LoadArgs(il, i);

                Type parmType = paramInfos[i-index].ParameterType;
                if (parmType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox_Any, parmType);
                }
            }

            if (method.IsStatic)
            {
                il.EmitCall(OpCodes.Call, method, null);
            }
            else
            {
                il.EmitCall(OpCodes.Callvirt, method, null);
            }

            //Return Value
            //if (method.ReturnType == typeof(void))
            //{
            //    il.Emit(OpCodes.Ldnull);
            //}
            //else
            //{
            //if (method.ReturnType.IsValueType)
            //{
            //    il.Emit(OpCodes.Box, method.ReturnType);
            //}
            //}
            il.Emit(OpCodes.Ret);

            return dym.CreateDelegate(delegateType);
        }

        public static Func<object> GetConstructor(ConstructorInfo constructor)
        {
            DynamicMethod ctor = new DynamicMethod(String.Empty, constructor.DeclaringType, null);
            var il = ctor.GetILGenerator();
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Ret);

            return ctor.CreateDelegate(typeof(Func<object>)) as Func<object>;
        }

        static void LoadArgs(ILGenerator il, int i)
        {
            switch (i)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    il.Emit(OpCodes.Ldarg_S, i);
                    break;
            }          
        }
    }
}
