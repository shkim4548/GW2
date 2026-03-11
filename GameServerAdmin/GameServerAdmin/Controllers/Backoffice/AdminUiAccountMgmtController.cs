using GameServerAdmin.Application.AdminAccount;
using GameServerAdmin.Models.Admin.AdminUi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerAdmin.Controllers.Backoffice;

[Authorize(Policy = "AdminOnly")]
public sealed class AdminUiAccountMgmtController : Controller
{
    private readonly IAdminAccountService _service;

    public AdminUiAccountMgmtController(IAdminAccountService service)
    {
        _service = service;
    }

    [HttpGet("/admin/ui/accounts")]
    public async Task<IActionResult> Index()
    {
        var list = await _service.GetAllAsync();
        return View(list);
    }

    [HttpGet("/admin/ui/accounts/create")]
    public IActionResult Create() => View(new CreateAdminViewModel());

    [HttpPost("/admin/ui/accounts/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAdminViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error) = await _service.CreateAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error!);
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/ui/accounts/{adminId:long}/deactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(long adminId)
    {
        await _service.SetActiveAsync(adminId, false);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/ui/accounts/{adminId:long}/activate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(long adminId)
    {
        await _service.SetActiveAsync(adminId, true);
        return RedirectToAction(nameof(Index));
    }
}
