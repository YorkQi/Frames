using Domain;
using Domain.Tasks;
using Domain.Tasks.Events;
using Xunit;

namespace Tests
{
    /// <summary>任务实体的状态流转与领域事件。</summary>
    public class TaskItemTests
    {
        [Fact]
        public void Create_DefaultsToTodo_AndKeepsUtcDueDate()
        {
            var dueDate = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Local);

            var task = TaskItem.Create(1, " 写文档 ", dueDate);

            Assert.Equal(1, task.ProjectId);
            Assert.Equal("写文档", task.Title);
            Assert.Equal(TaskItemStatus.Todo, task.Status);
            Assert.Equal(DateTimeKind.Utc, task.DueDate!.Value.Kind);
        }

        [Fact]
        public void Create_WithInvalidProjectId_Throws()
        {
            Assert.Throws<DomainException>(() => TaskItem.Create(0, "标题", null));
        }

        [Fact]
        public void Complete_SetsDone_AndRaisesCompletedEvent()
        {
            var task = TaskItem.Create(1, "写文档", null);

            task.Complete();

            Assert.Equal(TaskItemStatus.Done, task.Status);
            Assert.Contains(task.DomainEvents!, e =>
                e is TaskCompletedDomainEvent completed && completed.ProjectId == 1 && completed.Title == "写文档");
        }

        [Fact]
        public void Complete_Twice_Throws()
        {
            var task = TaskItem.Create(1, "写文档", null);
            task.Complete();

            Assert.Throws<DomainException>(task.Complete);
        }

        [Fact]
        public void Start_AfterDone_Throws()
        {
            var task = TaskItem.Create(1, "写文档", null);
            task.Complete();

            Assert.Throws<DomainException>(task.Start);
        }
    }
}
