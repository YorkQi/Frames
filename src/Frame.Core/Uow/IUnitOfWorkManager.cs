using System;

namespace Frame.Core.Uow
{
    /// <summary>
    /// 工作单元管理器接口。
    /// 负责创建和管理当前异步作用域（AsyncLocal）中的 UOW 实例。
    /// 支持嵌套 UOW：内层 UOW 自动附着到外层，由最外层统一提交或回滚。
    /// </summary>
    public interface IUnitOfWorkManager
    {
        /// <summary>
        /// 开始一个新的工作单元。
        /// </summary>
        /// <param name="options">UOW 配置选项。不传时使用默认配置。</param>
        /// <returns>工作单元实例。调用方应使用 using 包裹以确保 Dispose。</returns>
        IUnitOfWork Begin(UnitOfWorkOptions? options = null);

        /// <summary>
        /// 获取当前异步作用域中的活跃 UOW 实例。
        /// 可用于判断当前是否在 UOW 范围内，或获取外层 UOW 进行条件提交。
        /// </summary>
        IUnitOfWork? Current { get; }
    }
}
