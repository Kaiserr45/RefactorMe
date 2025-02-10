using Microsoft.EntityFrameworkCore;
using RefactorMe.Dal.Models;

namespace RefactorMe.Dal
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Survey> Surveys { get; set; }
        public DbSet<SurveyQuestion> SurveyQuestions { get; set; }
        public DbSet<SurveyResult> SurveyResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresEnum<SurveyQuestion.QuestionAnswerType>();

            // Если вопросы без опроса бессмысленны, можно поставить каскадное удаление
            // Если вопросы используются и в других опросах тоже, то ставим SetNull (думаю так логичнее)
            modelBuilder.Entity<Survey>()
                .HasMany(s => s.Questions)
                .WithOne()
                .HasForeignKey(q => q.SurveyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Запрещаем удаление пользователя, если с ним есть опросы, для избежания потери важных данных
            modelBuilder.Entity<SurveyResult>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(sr => sr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Аналогично с результатами опросов
            modelBuilder.Entity<SurveyResult>()
                .HasOne<Survey>()
                .WithMany()
                .HasForeignKey(sr => sr.SurveyId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
