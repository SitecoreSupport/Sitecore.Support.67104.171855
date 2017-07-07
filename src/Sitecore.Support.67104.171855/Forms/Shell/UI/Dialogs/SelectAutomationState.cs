using Sitecore.Diagnostics;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.WFFM.Abstractions.Dependencies;
using Sitecore.WFFM.Abstractions.Shared;
using System;

namespace Sitecore.Support.Forms.Shell.UI.Dialogs
{
  public class SelectAutomationState : DialogForm
  {
    protected Web.UI.XmlControls.XmlControl Dialog;
    private readonly IResourceManager resourceManager;
    protected Literal SelectSate;
    protected Controls.AutomationStateList StateList;

    public SelectAutomationState() : this(DependenciesManager.ResourceManager)
    {
    }

    public SelectAutomationState(IResourceManager resourceManager)
    {
      Assert.IsNotNull(resourceManager, "Dependency resourceManager is null");
      this.resourceManager = resourceManager;
    }

    protected virtual void Localize()
    {
      this.Dialog["Header"] = this.resourceManager.Localize("SELECT_ENGAGEMENT_PLAN");
      this.Dialog["Text"] = this.resourceManager.Localize("SELECT_AN_ENGAGEMENT_PLAN");
      this.SelectSate.Text = this.resourceManager.Localize("SELECT_STATE_AND_PLAN");
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      this.Localize();
    }

    protected override void OnOK(object sender, EventArgs args)
    {
      if (string.IsNullOrEmpty(this.StateList.Value))
      {
        SheerResponse.Alert(this.resourceManager.Localize("SELECT_ENGAGEMENT_PLAN_YOU_WANT_USE"), new string[0]);
      }
      else
      {
        SheerResponse.SetDialogValue(this.StateList.Value);
        base.OnOK(sender, args);
      }
    }

    protected override void OnPreRender(EventArgs e)
    {
      ((Button)Context.ClientPage.FindControl("OK")).KeyCode = string.Empty;
      base.OnPreRender(e);
    }

  }
}
