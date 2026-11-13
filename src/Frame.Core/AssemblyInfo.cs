using System.Runtime.CompilerServices;

// 测试需要访问 internal 的模块排序算法（SortModulesByDependency）
[assembly: InternalsVisibleTo("TestUnit")]
