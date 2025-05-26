using Microsoft.AspNetCore.Mvc;

using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

public class ShirtsController : Controller
{
    private readonly IWebApiExecuter _webApiExecuter;

    public ShirtsController(IWebApiExecuter webApiExecuter)
    {
        _webApiExecuter = webApiExecuter;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var shirts = await _webApiExecuter.InvokeGet<List<Shirt>>("shirts").ConfigureAwait(false);
        return View(shirts);
    }

    [HttpGet]
    public IActionResult CreateShirt()
    {
        return View(new Shirt());
    }

    [HttpPost]
    public async Task<IActionResult> CreateShirt(Shirt shirt)
    {
        if (!ModelState.IsValid)
            return View(shirt);

        try
        {
            var response = await _webApiExecuter.InvokePost("shirts", shirt).ConfigureAwait(false);
            if (response != null)
                TempData["Success"] = "Shirt created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (WebApiException ex)
        {
            AddErrorsToModelState(ex);
        }

        return View(shirt);
    }

    [HttpGet]
    public async Task<IActionResult> UpdateShirt(int shirtId)
    {
        try
        {
            var shirt = await _webApiExecuter.InvokeGet<Shirt>($"shirts/{shirtId}").ConfigureAwait(false);
            if (shirt == null)
                return NotFound();

            return View(shirt);
        }
        catch (WebApiException ex)
        {
            AddErrorsToModelState(ex);
            return View("Error");
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateShirt(Shirt shirt)
    {
        if (!ModelState.IsValid)
        {
            return View(shirt);
        }

        try
        {
            await _webApiExecuter.InvokePut($"shirts/{shirt.ShirtId}", shirt).ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }
        catch (WebApiException ex)
        {
            AddErrorsToModelState(ex);
        }

        return View(shirt);
    }

    public async Task<IActionResult> DeleteShirt(int shirtId)
    {

        try
        {
            await _webApiExecuter.InvokeDelete($"shirts/{shirtId}").ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }
        catch (WebApiException ex)
        {
            AddErrorsToModelState(ex);
            // return View(nameof(Index), await _webApiExecuter.InvokeGet<List<Shirt>>("shirts").ConfigureAwait(false));
            var shirts = await _webApiExecuter.InvokeGet<List<Shirt>>("shirts").ConfigureAwait(false);
            return View(nameof(Index), shirts);
        }
    }

    private void AddErrorsToModelState(WebApiException ex)
    {
        if (ex?.ErrorResponse?.Errors is null)
            return;

        foreach (var error in ex.ErrorResponse.Errors)
        {
            ModelState.AddModelError(error.Key, string.Join("; ", error.Value));
        }
    }

}
