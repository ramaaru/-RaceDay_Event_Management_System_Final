namespace RaceDay.API.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int? CategoryPosition { get; set; }
    public decimal? Pace { get; set; }
    public string ResultStatus { get; set; } = "Finished";
    public EventEnrolment? Enrolment { get; set; }
}
