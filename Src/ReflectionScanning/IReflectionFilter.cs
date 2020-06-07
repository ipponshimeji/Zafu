using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Zafu.ReflectionScanning {
	interface IReflectionFilter {
		Type[] GetTypes(Assembly assembly);
		FieldInfo[] GetFields(Type type);
		PropertyInfo[] GetProperties(Type type);
		MethodInfo[] GetMethods(Type type);
	}
}
