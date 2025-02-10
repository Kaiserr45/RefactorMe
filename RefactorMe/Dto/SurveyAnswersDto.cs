namespace RefactorMe.Dto;

public class SurveyAnswersDto
{
    public class SurveyAnswerDto
    {
        public int QuestionId { get; set; }
        public string Value { get; set; }
    }

    public int UserId { get; set; }
    public int SurveyId { get; set; }
    public SurveyAnswerDto[] Answers { get; set; }
}