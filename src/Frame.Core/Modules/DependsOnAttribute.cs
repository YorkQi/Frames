using System;

namespace Frame.Core.Modules
{
    /// <summary>
    /// 模块依赖声明特性。标注当前模块依赖的其他模块。
    /// 框架在初始化模块时将按依赖拓扑顺序执行各模块的生命周期方法。
    /// </summary>
    /// <remarks>
    /// 声明模块依赖。
    /// </remarks>
    /// <param name="dependedModuleTypes">所依赖的模块类型，必须实现 <see cref="IFrameModule"/></param>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DependsOnAttribute(params Type[] dependedModuleTypes) : Attribute
    {
        /// <summary>当前模块依赖的模块类型列表</summary>
        public Type[] DependedModuleTypes { get; } = dependedModuleTypes ?? throw new ArgumentNullException(nameof(dependedModuleTypes));
    }
}
