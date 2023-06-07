/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

namespace Azos
{
  /// <summary>
  /// Non-localizable constants
  /// </summary>
  public static class WebConsts
  {
    public const string HTTP_POST    = "POST";
    public const string HTTP_PUT     = "PUT";
    public const string HTTP_GET     = "GET";
    public const string HTTP_DELETE  = "DELETE";
    public const string HTTP_PATCH   = "PATCH";
    public const string HTTP_OPTIONS = "OPTIONS";


    public const string HTTP_HDR_AUTHORIZATION = "Authorization";
    public const string HTTP_HDR_ACCEPT = "Accept";
    public const string HTTP_HDR_USER_AGENT = "User-Agent";
    public const string HTTP_HDR_CONTENT_TYPE = "Content-Type";
    public const string HTTP_HDR_CONTENT_DISPOSITION = "Content-disposition";
    public const string HTTP_HDR_X_FORWARDED_FOR = "X-Forwarded-For";
    public const string HTTP_SET_COOKIE = "Set-Cookie";
    public const string HTTP_WWW_AUTHENTICATE = "WWW-Authenticate";

    public const string AUTH_SCHEME_BASIC = "Basic";
    public const string AUTH_SCHEME_BEARER = "Bearer";
    public const string AUTH_SCHEME_SYSTOKEN = "Systoken";

    public const int STATUS_200 = 200;  public const string STATUS_200_DESCRIPTION = "OK";

    public enum RedirectCode
    {
      MultipleChoices_300  = 300,
      MovedPermanently_301 = 301,
      Found_302            = 302,
      SeeOther_303         = 303,
      NotModified_304      = 304,
      UseProxy_305         = 305,
      SwitchProxy_306      = 306,
      Temporary_307        = 307,
      Permanent_308        = 308
    }

    public static int GetRedirectStatusCode(RedirectCode code)
    {
      return (int)code;
    }

    public static string GetRedirectStatusDescription(RedirectCode code)
    {
      switch(code)
      {
        case RedirectCode.MultipleChoices_300 : return "Multiple Choices";
        case RedirectCode.MovedPermanently_301: return "Moved Permanently";
        case RedirectCode.Found_302           : return "Found";
        case RedirectCode.SeeOther_303        : return "See Other";
        case RedirectCode.NotModified_304     : return "Not Modified";
        case RedirectCode.UseProxy_305        : return "Use Proxy";
        case RedirectCode.SwitchProxy_306     : return "Switch Proxy";
        case RedirectCode.Temporary_307       : return "Temporary Redirect";
        case RedirectCode.Permanent_308       : return "Permanent Redirect";
        default: return "Other";
      }
    }

    public const int STATUS_404 = 404;  public const string STATUS_404_DESCRIPTION = "Not found";
    public const int STATUS_403 = 403;  public const string STATUS_403_DESCRIPTION = "Forbidden";


    public const int STATUS_400 = 400;  public const string STATUS_400_DESCRIPTION = "Bad Request";

    public const int STATUS_401 = 401;  public const string STATUS_401_DESCRIPTION = "Unauthorized";

    public const int STATUS_405 = 405;  public const string STATUS_405_DESCRIPTION = "Method Not Allowed";

    public const int STATUS_406 = 406;  public const string STATUS_406_DESCRIPTION = "Not Acceptable";

    public const int STATUS_409 = 409;  public const string STATUS_409_DESCRIPTION = "Conflict";

    public const int STATUS_411 = 411; public const string STATUS_411_DESCRIPTION = "Content-Length Required";

    public const int STATUS_413 = 413; public const string STATUS_413_DESCRIPTION = "Request Entity Too Large";

    //https://www.rfc-editor.org/rfc/rfc4918#section-11.2
    public const int STATUS_422 = 422; public const string STATUS_422_DESCRIPTION = "Unprocessable Entity";

    public const int STATUS_429 = 429;  public const string STATUS_429_DESCRIPTION = "Too Many Requests";

    public const int STATUS_500 = 500;  public const string STATUS_500_DESCRIPTION = "Internal Error";
  }
}
