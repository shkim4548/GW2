using GameServerAdmin.Common.Exceptions;
using GameServerAdmin.Common.Exceptions.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GameServerAdmin.Application.Validation
{
    public class ModelStateValidationFilter : IActionFilter
    {
        //public void OnActionExecuted(ActionExecutedContext context)
        //{
        //    if(context.ModelState.IsValid)
        //    {
        //        return;
        //    }

        //    var fieldErrors = new FieldErrorCollection();

        //    foreach(var (key, state) in context.ModelState)
        //    {
        //        foreach(var error in state.Errors)
        //        {
        //            fieldErrors.AddError(key, string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value" : error.ErrorMessage);
        //        }
        //    }
        //    throw new RequestValidationException(fieldErrors);
        //}

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid)
                return;

            var fieldErrors = new FieldErrorCollection();
            foreach(var (key, state) in context.ModelState)
            {
                foreach(var error in state.Errors)
                {
                    fieldErrors.AddError(key, string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Invalid value" : error.ErrorMessage);
                }
            }
            throw new RequestValidationException(fieldErrors);
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
