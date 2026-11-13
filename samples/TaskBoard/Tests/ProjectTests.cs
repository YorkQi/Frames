using Domain;
using Domain.Projects;
using Domain.Projects.Events;
using Xunit;

namespace Tests
{
    /// <summary>项目聚合的规则与领域事件。</summary>
    public class ProjectTests
    {
        [Fact]
        public void Create_NormalizesFields_AndRaisesCreatedEvent()
        {
            var project = Project.Create(" demo ", "演示项目", " 描述 ", "user-1");

            Assert.Equal("DEMO", project.Code);
            Assert.Equal("演示项目", project.Name);
            Assert.Equal("描述", project.Description);
            Assert.Equal("user-1", project.OwnerId);
            Assert.Equal(ProjectStatus.Active, project.Status);

            Assert.Contains(project.DomainEvents!, e =>
                e is ProjectCreatedDomainEvent created && created.Code == "DEMO" && created.Name == "演示项目");
        }

        [Theory]
        [InlineData("", "名称")]
        [InlineData("CODE", "")]
        public void Create_WithEmptyRequiredField_Throws(string code, string name)
        {
            Assert.Throws<DomainException>(() => Project.Create(code, name, null, "user-1"));
        }

        [Fact]
        public void Rename_ChangesName_AndRaisesRenamedEvent()
        {
            var project = Project.Create("DEMO", "旧名称", null, "user-1");

            project.Rename(" 新名称 ");

            Assert.Equal("新名称", project.Name);
            Assert.Contains(project.DomainEvents!, e => e is ProjectRenamedDomainEvent renamed && renamed.Name == "新名称");
        }

        [Fact]
        public void Rename_WithSameName_DoesNotRaiseEvent()
        {
            var project = Project.Create("DEMO", "名称", null, "user-1");

            project.Rename("名称");

            Assert.DoesNotContain(project.DomainEvents!, e => e is ProjectRenamedDomainEvent);
        }

        [Fact]
        public void Archive_SetsStatus_AndRaisesArchivedEvent()
        {
            var project = Project.Create("DEMO", "名称", null, "user-1");

            project.Archive();

            Assert.Equal(ProjectStatus.Archived, project.Status);
            Assert.Contains(project.DomainEvents!, e => e is ProjectArchivedDomainEvent archived && archived.Code == "DEMO");
        }

        [Fact]
        public void Archive_Twice_Throws()
        {
            var project = Project.Create("DEMO", "名称", null, "user-1");
            project.Archive();

            Assert.Throws<DomainException>(project.Archive);
        }

        [Fact]
        public void Rename_WhenArchived_Throws()
        {
            var project = Project.Create("DEMO", "名称", null, "user-1");
            project.Archive();

            Assert.Throws<DomainException>(() => project.Rename("另一个名称"));
        }
    }
}
