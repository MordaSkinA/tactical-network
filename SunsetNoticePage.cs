using System.IO;


public static class SunsetNoticePage
{
    public const string RepoUrl = "https://github.com/MordaSkinA/tactical-network";


    private const string LeftImageFile = "notice-left.png";
    private const string RightImageFile = "notice-right.png";

    public static string GetHtml(string webRootPath)
    {
        string leftImg = BuildImageTag(webRootPath, LeftImageFile, "side-img side-img-left");
        string rightImg = BuildImageTag(webRootPath, RightImageFile, "side-img side-img-right");

        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>Tacnet</title>
<style>
  :root {
    --bg: #0a0a0b;
    --surface: #141416;
    --border: #262629;
    --accent: #7c6cf0;
    --accent-light: #9b8ff5;
    --text: #f0f0f2;
    --text-dim: #9a9aa2;
    --text-faint: #636368;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    min-height: 100vh;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 20px;
    background: var(--bg);
    color: var(--text);
    font-family: 'Inter', system-ui, -apple-system, sans-serif;
    overflow-x: hidden;
  }
  .row {
    display: flex;
    align-items: flex-end;
    justify-content: center;
  }
  .side-img {
    max-height: 380px;
    max-width: 230px;
    width: auto;
    height: auto;
    object-fit: contain;
    flex-shrink: 0;
    position: relative;
    z-index: 1;
  }
  .side-img-left { margin-right: -70px; }
  .side-img-right { margin-left: -70px; }
  @media (max-width: 900px) {
    .side-img { display: none; }
  }
  .card {
    width: 100%;
    max-width: 460px;
    background: var(--surface);
    border: 1px solid var(--border);
    border-radius: 18px;
    padding: 34px 32px 30px;
    position: relative;
    z-index: 2;
  }
  #lang-toggle-btn {
    position: absolute;
    top: 20px;
    right: 20px;
    background: transparent;
    border: 1px solid var(--border);
    color: var(--text-dim);
    border-radius: 999px;
    padding: 6px 14px;
    font-size: 12px;
    font-weight: 700;
    cursor: pointer;
    letter-spacing: 0.04em;
  }
  #lang-toggle-btn:hover { color: var(--text); border-color: var(--text-faint); }
  h1 { font-size: 18px; margin: 0 0 14px; text-align: center; padding-right: 60px; }
  p { color: var(--text-dim); font-size: 14.5px; line-height: 1.55; margin: 0 0 22px; text-align: center; }
  a.repo-btn {
    display: block;
    width: 100%;
    padding: 13px;
    border-radius: 999px;
    background: var(--accent);
    color: #fff;
    text-decoration: none;
    font-weight: 700;
    font-size: 14.5px;
    text-align: center;
    transition: filter 0.12s ease;
  }
  a.repo-btn:hover { filter: brightness(1.12); }
</style>
</head>
<body>
  <div class=""row"">
  " + leftImg + @"
  <div class=""card"">
    <button type=""button"" id=""lang-toggle-btn"" onclick=""toggleLang()"">EN</button>
    <h1 id=""heading"">This project is no longer hosted here</h1>
    <p id=""body-text"">The server at this address is no longer maintained. The full source is open - download the repository and self-host it (requires .NET 8, instructions in the README).</p>
    <a class=""repo-btn"" id=""repo-btn"" href=""" + RepoUrl + @""" target=""_blank"" rel=""noopener"">Open repository on GitHub</a>
  </div>
  " + rightImg + @"
  </div>

  <script>
    const STRINGS = {
      en: {
        heading: 'This project is no longer hosted here',
        body: 'The server at this address is no longer maintained. The full source is open - download the repository and self-host it (requires .NET 8, instructions in the README).',
        btn: 'Open repository on GitHub'
      },
      vi: {
        heading: 'Dự án này không còn được host ở đây nữa',
        body: 'Máy chủ tại địa chỉ này không còn được duy trì. Toàn bộ mã nguồn đã mở - bạn có thể tải repository về và tự host (cần .NET 8, hướng dẫn trong README).',
        btn: 'Mở repository trên GitHub'
      }
    };

    let lang = 'en';

    function render() {
      const s = STRINGS[lang];
      document.getElementById('heading').textContent = s.heading;
      document.getElementById('body-text').textContent = s.body;
      document.getElementById('repo-btn').textContent = s.btn;
      document.getElementById('lang-toggle-btn').textContent = lang === 'en' ? 'VI' : 'EN';
    }

    function toggleLang() {
      lang = lang === 'en' ? 'vi' : 'en';
      render();
    }

    render();
  </script>
</body>
</html>";
    }

    private static string BuildImageTag(string webRootPath, string fileName, string cssClass)
    {
        try
        {
            var path = Path.Combine(webRootPath ?? "", fileName);
            if (!File.Exists(path)) return "";

            var bytes = File.ReadAllBytes(path);
            var base64 = Convert.ToBase64String(bytes);
            return "<img class=\"" + cssClass + "\" src=\"data:image/png;base64," + base64 + "\" alt=\"\">";
        }
        catch
        {
            return "";
        }
    }
}
