// Services/EmailTemplates.cs
namespace ScspApi.Services;

public static class EmailTemplates
{
    private const string Brand = "SCS";
    private const string Primary = "#2563eb";   // azul
    private const string Text = "#111827";      // slate-900
    private const string Muted = "#6b7280";     // slate-500
    private const string Bg = "#f8fafc";        // slate-50
    private const string Card = "#ffffff";

    private static string Wrap(string title, string inner)
    {
        return $@"
<!doctype html>
<html lang=""pt-br"">
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width,initial-scale=1"">
  <title>{title}</title>
</head>
<body style=""margin:0;padding:0;background:{Bg};"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:{Bg};padding:24px 0"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;background:{Card};border-radius:12px;box-shadow:0 1px 4px rgba(0,0,0,.06);overflow:hidden"">
          <tr>
            <td style=""padding:20px 24px;background:{Primary};color:#fff;font-family:Segoe UI,Roboto,Arial,sans-serif;font-size:20px;font-weight:600"">
              {Brand}
            </td>
          </tr>
          <tr>
            <td style=""padding:24px;font-family:Segoe UI,Roboto,Arial,sans-serif;color:{Text};font-size:16px;line-height:1.6"">
              {inner}
              <hr style=""border:none;border-top:1px solid #e5e7eb;margin:24px 0""/>
              <p style=""color:{Muted};font-size:13px;margin:0"">
                Se você não solicitou esta ação, ignore este e-mail.
              </p>
            </td>
          </tr>
          <tr>
            <td style=""padding:16px 24px;background:#f1f5f9;color:{Muted};font-family:Segoe UI,Roboto,Arial,sans-serif;font-size:12px"">
              © {DateTime.UtcNow:yyyy} {Brand}. Todos os direitos reservados.
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }

    public static string EmailConfirmationLink(string userFirstName, string confirmUrl)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Confirme seu e-mail</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")},</p>
<p>Para concluir seu cadastro e proteger sua conta, confirme seu endereço de e-mail.</p>
<p style=""margin:20px 0"">
  <a href=""{confirmUrl}"" style=""display:inline-block;background:{Primary};color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600"">
    Confirmar e-mail
  </a>
</p>
<p style=""color:{Muted};font-size:14px;margin-top:16px"">Ou copie e cole este link no navegador:<br>
  <span style=""word-break:break-all"">{confirmUrl}</span>
</p>";
        return Wrap("Confirme seu e-mail", inner);
    }

    public static string EmailConfirmationCode(string userFirstName, string code, int minutes)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Código de confirmação</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")}, use o código abaixo para confirmar seu e-mail:</p>
<div style=""margin:16px 0;padding:14px 18px;border:1px dashed {Primary};border-radius:10px;display:inline-block;font-size:24px;font-weight:700;letter-spacing:2px"">
  {code}
</div>
<p style=""color:{Muted};font-size:14px;margin-top:8px"">O código expira em {minutes} minutos.</p>";
        return Wrap("Código de confirmação", inner);
    }

    public static string EmailConfirmationLinkAndCode(string userFirstName, string confirmUrl, string code, int minutes)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Confirme seu e-mail</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")},</p>
<p>Você pode confirmar seu e-mail de duas maneiras:</p>
<ol>
  <li><b>Clicando no botão</b> abaixo:</li>
</ol>
<p style=""margin:16px 0"">
  <a href=""{confirmUrl}"" style=""display:inline-block;background:{Primary};color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600"">
    Confirmar e-mail
  </a>
</p>
<ol start=""2"">
  <li><b>Ou digitando o código</b> no aplicativo:</li>
</ol>
<div style=""margin:12px 0;padding:14px 18px;border:1px dashed {Primary};border-radius:10px;display:inline-block;font-size:24px;font-weight:700;letter-spacing:2px"">
  {code}
</div>
<p style=""color:{Muted};font-size:14px;margin-top:8px"">O código expira em {minutes} minutos.</p>
<p style=""color:{Muted};font-size:14px;margin-top:16px"">Se preferir, copie o link:<br>
  <span style=""word-break:break-all"">{confirmUrl}</span>
</p>";
        return Wrap("Confirme seu e-mail", inner);
    }


    public static string PasswordResetLink(string userFirstName, string resetUrl)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Redefinir senha</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")}, clique no botão para redefinir sua senha.</p>
<p style=""margin:20px 0"">
  <a href=""{resetUrl}"" style=""display:inline-block;background:{Primary};color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600"">
    Redefinir senha
  </a>
</p>
<p style=""color:{Muted};font-size:14px;margin-top:16px"">Ou use o link direto:<br>
  <span style=""word-break:break-all"">{resetUrl}</span>
</p>";
        return Wrap("Redefinir senha", inner);
    }

    public static string PasswordResetLinkAndCode(string userFirstName, string resetUrl, string code, int minutes)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Redefinir senha</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")}, você pode redefinir sua senha de duas maneiras:</p>
<ol>
  <li><b>Clicando no botão</b> abaixo:</li>
</ol>
<p style=""margin:16px 0"">
  <a href=""{resetUrl}"" style=""display:inline-block;background:{Primary};color:#fff;text-decoration:none;padding:12px 18px;border-radius:8px;font-weight:600"">
    Redefinir senha
  </a>
</p>
<ol start=""2"">
  <li><b>Ou digitando o código</b> no aplicativo:</li>
</ol>
<div style=""margin:12px 0;padding:14px 18px;border:1px dashed {Primary};border-radius:10px;display:inline-block;font-size:24px;font-weight:700;letter-spacing:2px"">
  {code}
</div>
<p style=""color:{Muted};font-size:14px;margin-top:8px"">O código expira em {minutes} minutos.</p>
<p style=""color:{Muted};font-size:14px;margin-top:16px"">Se preferir, copie o link:<br>
  <span style=""word-break:break-all"">{resetUrl}</span>
</p>";
        return Wrap("Redefinir senha", inner);
    }


    public static string PasswordResetCode(string userFirstName, string code, int minutes)
    {
        var inner = $@"
<h1 style=""margin:0 0 8px;font-size:22px"">Código para redefinir senha</h1>
<p>Olá{(string.IsNullOrWhiteSpace(userFirstName) ? "" : $" {userFirstName}")}, use o código abaixo para redefinir sua senha:</p>
<div style=""margin:16px 0;padding:14px 18px;border:1px dashed {Primary};border-radius:10px;display:inline-block;font-size:24px;font-weight:700;letter-spacing:2px"">
  {code}
</div>
<p style=""color:{Muted};font-size:14px;margin-top:8px"">O código expira em {minutes} minutos.</p>";
        return Wrap("Código para redefinir senha", inner);
    }
}
