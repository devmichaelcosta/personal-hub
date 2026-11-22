using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PersonalHub.Web.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ModelStateValidatorAttribute : Attribute, IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                foreach (var item in context.ModelState)
                    if (item.Value.Errors.Count > 0)
                        foreach (var error in item.Value.Errors)
                            context.ModelState.AddModelError(string.Empty, error.ErrorMessage);

                if (context.Controller is Controller controller)
                {
                    controller.ViewBag.TemErros = true;
                }

                //object? currentModel = context.ActionArguments.Any(e => e.Key == "model")
                //    ? context.ActionArguments.SingleOrDefault(e => e.Key == "model").Value
                //    : null;

                //context.Result = new ViewResult() 
                //{ 
                //    ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(((Controller)context.Controller).ViewData)
                //    {
                //        Model = currentModel
                //    }
                //};
                //context.Result = new ViewResult()
                //{
                //    ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(((Controller)context.Controller).ViewData)
                //    {
                //        Model = context.ModelState.Values
                //    }
                //};
            }
        }
    }
}
