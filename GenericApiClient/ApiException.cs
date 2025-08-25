namespace GenericApiClient;

public class ApiException(ApiProblemDetails problemDetails) : Exception(problemDetails.Detail ?? problemDetails.Title)
{
    public ApiProblemDetails ProblemDetails { get; } = problemDetails;
}