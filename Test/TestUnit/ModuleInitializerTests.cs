using Frame.Core;
using Frame.Core.Modules;
using Xunit;

namespace TestUnit
{
    public class ModuleInitializerTests
    {
        private sealed class RecordingModule : IFrameModule
        {
            private readonly List<string> _log;

            public RecordingModule(string name, List<string> log)
            {
                Name = name;
                _log = log;
            }

            public string Name { get; }

            public void PreConfigureServices(ServiceConfigurationContext context)
            {
            }

            public void ConfigureServices(ServiceConfigurationContext context)
            {
            }

            public void PostConfigureServices(ServiceConfigurationContext context)
            {
            }

            public Task OnApplicationInitializationAsync(CancellationToken cancellationToken = default)
            {
                _log.Add($"{Name}:init");
                return Task.CompletedTask;
            }

            public Task OnApplicationShutdownAsync(CancellationToken cancellationToken = default)
            {
                _log.Add($"{Name}:shutdown");
                return Task.CompletedTask;
            }
        }

        private sealed class ModuleA : FrameModule
        {
        }

        [DependsOn(typeof(ModuleA))]
        private sealed class ModuleB : FrameModule
        {
        }

        [DependsOn(typeof(ModuleB))]
        private sealed class ModuleC : FrameModule
        {
        }

        private sealed class IndependentModule : FrameModule
        {
        }

        [Fact]
        public async Task InitializeAsync_CallsHooksInGivenOrder()
        {
            var log = new List<string>();
            var initializer = new ModuleInitializer(new IFrameModule[]
            {
                new RecordingModule("first", log),
                new RecordingModule("second", log)
            });

            await initializer.InitializeAsync();

            Assert.Equal(new[] { "first:init", "second:init" }, log);
        }

        [Fact]
        public async Task ShutdownAsync_CallsHooksInReverseOrder()
        {
            var log = new List<string>();
            var initializer = new ModuleInitializer(new IFrameModule[]
            {
                new RecordingModule("first", log),
                new RecordingModule("second", log)
            });

            await initializer.ShutdownAsync();

            Assert.Equal(new[] { "second:shutdown", "first:shutdown" }, log);
        }

        [Fact]
        public void Ctor_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => new ModuleInitializer(null!));
        }

        [Fact]
        public void SortModulesByDependency_PlacesDependenciesFirst()
        {
            var sorted = ServiceDependencyInjection.SortModulesByDependency(new List<Type>
            {
                typeof(ModuleC),
                typeof(ModuleB),
                typeof(ModuleA),
                typeof(IndependentModule)
            });

            Assert.True(sorted.IndexOf(typeof(ModuleA)) < sorted.IndexOf(typeof(ModuleB)));
            Assert.True(sorted.IndexOf(typeof(ModuleB)) < sorted.IndexOf(typeof(ModuleC)));
            Assert.Contains(typeof(IndependentModule), sorted);
        }
    }
}
