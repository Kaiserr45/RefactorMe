using Microsoft.EntityFrameworkCore;
using RefactorMe.Dal;
using RefactorMe.Dal.Models;
using RefactorMe.Dto;

namespace RefactorMe;

public class SurveyService
{
    private readonly AppDbContext _db;

    // Program.cs в проекте нет, поэтому предполагаю, что прописывать в DI-контейнере зависимости в рамках задачи не требуется
    public SurveyService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получение опросов для пользователя
    /// </summary>
    public async Task<SurveyDto[]> GetSurveys(int userId)
    {
        // Оптимизировали запрос для большого количества данных, так как до этого использовали Any внутри Where.
        // Если знаем, что данных немного, можно оставить как было.
        return await _db.Surveys
            .Include(x => x.Questions)
            .GroupJoin(_db.SurveyResults.Where(sr => sr.UserId == userId),
                survey => survey.Id,
                result => result.SurveyId,
                (survey, results) => new { survey, results })
            .Where(x => x.survey.IsActive && !x.results.Any())
            .Select(x => new SurveyDto()
            {
                Id = x.survey.Id,
                Questions = x.survey.Questions
                    .Select(q => new SurveyDto.SurveyQuestionDto()
                    {
                        Id = q.Id,
                        Text = q.Text,
                        Type = q.AnswerType
                    }).ToArray()
            }).ToArrayAsync();
    }

    /// <summary>
    /// Сохранение результатов опроса
    /// </summary>
    public async Task SaveAnswers(SurveyAnswersDto value)
    {
        await using var tr = await _db.Database.BeginTransactionAsync();

        if (value.Answers == null || value.Answers.Length == 0)
        {
            throw new ArgumentException("Ответы не могут быть пустыми.");
        }

        var questions = await _db.SurveyQuestions
            .Where(q => value.Answers.Select(a => a.QuestionId).Contains(q.Id))
            .ToListAsync();

        if (questions.Count != value.Answers.Length)
        {
            throw new ArgumentException("Некоторые вопросы не найдены.");
        }

        int score = 0;

        foreach (var answer in value.Answers)
        {
            var question = questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null)
            {
                throw new ArgumentException($"Вопрос с ID {answer.QuestionId} не найден.");
            }

            switch (question.AnswerType)
            {
                case SurveyQuestion.QuestionAnswerType.Boolean:
                    if (bool.TryParse(answer.Value, out bool boolValue) && boolValue)
                        score++;
                    break;

                case SurveyQuestion.QuestionAnswerType.Number:
                    if (int.TryParse(answer.Value, out int intValue) && intValue > question.NumberMin)
                        score++;
                    break;

                case SurveyQuestion.QuestionAnswerType.SingleChoice:
                    if (int.TryParse(answer.Value, out int selectedId) && question.CorrectAnswerId == selectedId)
                        score++;
                    break;
            }
        }

        await _db.SurveyResults.AddAsync(new SurveyResult()
        {
            UserId = value.UserId,
            SurveyId = value.SurveyId,
            Score = score,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        await tr.CommitAsync();
    }
}