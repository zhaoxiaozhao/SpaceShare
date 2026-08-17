namespace FriendlySeat.Application.Common;

public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string code, string message, int statusCode = 400) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public static AppException BadRequest(string code, string message) =>
        new(code, message, 400);

    public static AppException Unauthorized(string message = "未登录或登录已过期") =>
        new("unauthorized", message, 401);

    public static AppException Forbidden(string message = "没有权限执行此操作") =>
        new("forbidden", message, 403);

    public static AppException NotFound(string message = "资源不存在") =>
        new("not_found", message, 404);

    public static AppException Conflict(string code, string message) =>
        new(code, message, 409);
}
