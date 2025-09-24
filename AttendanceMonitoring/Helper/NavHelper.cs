using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace AttendanceMonitoring.Helper
{
    public static class NavHelper
    {                                  //Tag Helper                             //use params to store multiple variables into an array
        public static string IsActive(this IHtmlHelper html, string controller, params string[] action)
        {
            //You can then use these helpers in your Razor view: Para magamit mona si method na IsActive sa Razor para maretrieve mo yung info ng controller, etc sa razor pagewith the help of Tag helper
            var RouteData = html.ViewContext.RouteData;
            //to retrieve the name of the current action method being executed in a controller. 
            var RouteAction = (string)RouteData.Values["action"];
            var RouteController = (string)RouteData.Values["controller"];
                            //Checks if the current page matches the controller AND any of the provided actions.
            bool isActive = controller == RouteController && action.Contains(RouteAction); action.Contains(RouteAction); //action.Contains(RouteAction)mimics PHP’s in_array()—it checks if the current action is one of the expected ones.
            //ternary operator, like an if-else
            return isActive ? "active" : ""; //means condition ? trueResult : falseResult
                   //↑ is like if(isActive)
                   //            return "active"
                   //          else
                   //            return ""
        }
    }
}
