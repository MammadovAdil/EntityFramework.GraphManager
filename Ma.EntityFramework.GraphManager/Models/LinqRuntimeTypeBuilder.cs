using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Ma.EntityFramework.GraphManager.Models
{
    /// <summary>
    /// This class is for creating types as anonymous type at runtime.
    /// <remarks>
    /// This class is copied from StackOverflow answer (http://stackoverflow.com/a/723018/1380428).
    /// Alterations and additions made by Adil Mammadov.
    /// </remarks>
    /// </summary>
    public static class LinqRuntimeTypeBuilder
    {
        private static AssemblyName assemblyName = new AssemblyName() { Name = "DynamicLinqTypes" };
        private static ModuleBuilder moduleBuilder = null;
        private static Dictionary<string, Type> builtTypes = new Dictionary<string, Type>();

        static LinqRuntimeTypeBuilder()
        {
            moduleBuilder = Thread
                .GetDomain()
                .DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                .DefineDynamicModule(assemblyName.Name);
        }

        /// <summary>
        /// Create key to identify dynamic type by using it as name
        /// </summary>
        /// <param name="properties">Propeties of class</param>
        /// <returns>Key to identify class</returns>
        private static string GetTypeKey(Dictionary<string, Type> properties)
        {
            string key = string.Empty;
            foreach (var field in properties.OrderBy(f => f.Key))
                key += field.Key + ";" + field.Value.Name + ";";

            return key;
        }

        /// <summary>
        /// Get dynamic type according to fields. If such type already exists gets that,
        /// otherwise creates new one and stores it for further use
        /// </summary>
        /// <param name="properties">Types and names of Propeties to create dynamic type according to</param>
        /// <exception cref="ArgumentNullException">
        /// When properties argument is null
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// When properties dictionary has no item inside
        /// </exception>
        /// <returns>Dynamically type</returns>
        public static Type GetDynamicType(Dictionary<string, Type> properties)
        {
            if (null == properties)
                throw new ArgumentNullException("properties");
            if (0 == properties.Count)
                throw new ArgumentOutOfRangeException("properties", "properties must have at least 1 proeprty definition");

            try
            {
                Monitor.Enter(builtTypes);
                string className = GetTypeKey(properties);

                if (builtTypes.ContainsKey(className))
                    return builtTypes[className];

                TypeBuilder typeBuilder = moduleBuilder.DefineType(className,
                    TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

                foreach (var field in properties)
                    typeBuilder.GenerateProperty(field);

                builtTypes[className] = typeBuilder.CreateType();

                return builtTypes[className];
            }
            finally
            {
                Monitor.Exit(builtTypes);
            }
        }

        /// <summary>
        /// Create key to identify dynamic type by using it as name
        /// </summary>
        /// <param name="properties">Properties to create type key according to</param>
        /// <returns>Key to identify class</returns>
        private static string GetTypeKey(IEnumerable<PropertyInfo> properties)
        {
            return GetTypeKey(properties.ToDictionary(f => f.Name, f => f.PropertyType));
        }

        /// <summary>
        /// Get dynamic type according to fields. If such type already exists gets that,
        /// otherwise creates new one and stores it for further use
        /// </summary>
        /// <param name="properties">Properties to create type according to</param>
        /// <returns>Dynamically type</returns>
        public static Type GetDynamicType(IEnumerable<PropertyInfo> properties)
        {
            return GetDynamicType(properties.ToDictionary(f => f.Name, f => f.PropertyType));
        }

        /// <summary>
        /// Generate private field and public property on typeBuilder according to propertyDetails
        /// </summary>
        /// <remarks>
        /// This was written by Adil Mammadov 
        /// according to web page https://msdn.microsoft.com/en-us/library/2sd82fz7(v=vs.110).aspx.
        /// The purpose of this code is adding property instead of field to runtime generated type.
        /// Adding field to type is very easy, adding property is my personal preference.
        /// If you prefer adding field and avoiding this long piece of code, you can do it simply by
        /// following code:
        ///     typeBuilder.DefineField(propertyDetails.Key, propertyDetails.Value, FieldAttributes.Public);
        /// </remarks>
        /// <param name="typeBuilder">Type builder to ad property to</param>
        /// <param name="propertyDetails">Details of property</param>
        /// <returns>Generated property</returns>
        public static PropertyBuilder GenerateProperty(
            this TypeBuilder typeBuilder, KeyValuePair<string, Type> propertyDetails)
        {

            FieldBuilder fieldBuilder = typeBuilder
                                        .DefineField(
                                            string.Format("_{0}", propertyDetails.Key.ToLower()),
                                            propertyDetails.Value,
                                            FieldAttributes.Private);


            // The last argument of DefineProperty is null, because the 
            // property has no parameters. (If you don't specify null, you must 
            // specify an array of Type objects. For a parameterless property, 
            // use an array with no elements: new Type[] {})
            PropertyBuilder propertyBuilder =
                typeBuilder.DefineProperty(propertyDetails.Key,
                                        PropertyAttributes.None,
                                        propertyDetails.Value,
                                        null);

            // The property set and property get methods require a special 
            // set of attributes.
            MethodAttributes getSetAttr =
                MethodAttributes.Public | MethodAttributes.SpecialName
                | MethodAttributes.HideBySig;

            // Define the "get" accessor method for CustomerName.
            MethodBuilder propertyGetMethodBuilder =
                typeBuilder.DefineMethod(string.Format("get_{0}", propertyDetails.Key),
                                            getSetAttr,
                                            propertyDetails.Value,
                                            Type.EmptyTypes);

            ILGenerator propertyGetIL = propertyGetMethodBuilder.GetILGenerator();

            propertyGetIL.Emit(OpCodes.Ldarg_0);
            propertyGetIL.Emit(OpCodes.Ldfld, fieldBuilder);
            propertyGetIL.Emit(OpCodes.Ret);

            // Define the "set" accessor method for property.
            MethodBuilder propertySetMethodBuilder =
                typeBuilder.DefineMethod(string.Format("set_{0}", propertyDetails.Key),
                                            getSetAttr,
                                            null,
                                            new Type[] { propertyDetails.Value });

            ILGenerator propertySetIL = propertySetMethodBuilder.GetILGenerator();

            propertySetIL.Emit(OpCodes.Ldarg_0);
            propertySetIL.Emit(OpCodes.Ldarg_1);
            propertySetIL.Emit(OpCodes.Stfld, fieldBuilder);
            propertySetIL.Emit(OpCodes.Ret);

            // Last, we must map the two methods created above to our PropertyBuilder to  
            // their corresponding behaviors, "get" and "set" respectively. 
            propertyBuilder.SetGetMethod(propertyGetMethodBuilder);
            propertyBuilder.SetSetMethod(propertySetMethodBuilder);

            return propertyBuilder;
        }
    }
}
