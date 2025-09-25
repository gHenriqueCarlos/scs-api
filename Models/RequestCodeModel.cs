namespace ScspApi.Models;

public class RequestCodeModel
{
    public string Email { get; set; } = default!;
}

public class ConfirmEmailWithCodeModel
{
    public string Email { get; set; } = default!;
    public string Code { get; set; } = default!;
}

public class ResetPasswordWithCodeModel
{
    public string Email { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string NewPassword { get; set; } = default!;
}
