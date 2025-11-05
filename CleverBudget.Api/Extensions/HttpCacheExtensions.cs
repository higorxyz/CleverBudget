using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace CleverBudget.Api.Extensions;

public static class HttpCacheExtensions
{
    public static bool RequestHasMatchingEtag(this ControllerBase controller, string etag)
    {
        if (string.IsNullOrEmpty(etag))
        {
            return false;
        }

        if (!controller.Request.Headers.TryGetValue("If-None-Match", out StringValues values))
        {
            return false;
        }

        foreach (var value in values)
        {
            if (value != null && string.Equals(value.Trim('"'), etag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetEtagHeader(this ControllerBase controller, string etag)
    {
        if (string.IsNullOrEmpty(etag))
        {
            return;
        }

        controller.Response.Headers["ETag"] = $"\"{etag}\"";
    }

    public static IActionResult CachedStatus(this ControllerBase controller)
    {
        return controller.StatusCode(StatusCodes.Status304NotModified);
    }
}
