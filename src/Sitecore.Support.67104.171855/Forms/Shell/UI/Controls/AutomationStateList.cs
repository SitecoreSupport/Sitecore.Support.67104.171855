using Newtonsoft.Json;
using Sitecore.Automation;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Form.Core.Configuration;
using Sitecore.Form.Web.UI.Controls;
using Sitecore.Forms.Shell.UI.Controls;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.WFFM.Abstractions.Dependencies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.UI;

namespace Sitecore.Support.Forms.Shell.UI.Controls
{
  public class AutomationStateList : XmlControl
  {
    protected Scrollbox Container;

    public AutomationStateList()
    {
      this.ID = "StateList";
    }

    private IEnumerable<Item> Filter(string search)
    {
      Func<Item, bool> predicate = null;
      Item root = Factory.GetDatabase("master").GetItem(FormIDs.Analytics.EngagementPlanRoot);

      IEnumerable<Item> automationPlans = this.GetAutomationPlans(root, null);

      if (string.IsNullOrEmpty(search))
      {
        return automationPlans;
      }
      search = search.ToLower();

      if (predicate == null)
      {
        predicate = plan => this.IsChildNameContainsPattern(plan, search);
      }

      return automationPlans.Where<Item>(predicate);
    }

    private IEnumerable<Item> GetAutomationPlans(Item root, List<Item> plans)
    {
      //modified part
      bool showOnlyChildren = false;
      string doNotShowFolder = Sitecore.Configuration.Settings.GetSetting("EnrollInEngagementPlan.DoNotShowFolder");
      bool.TryParse(Sitecore.Configuration.Settings.GetSetting("EnrollInEngagementPlan.ShowOnlyChildren"), out showOnlyChildren);
      
      Assert.ArgumentNotNull(root, "root");
      if (plans == null)
      {
        plans = new List<Item>();
      }
      foreach (Item item in root.Children)
      {
        if (item.TemplateID == AutomationIds.EngagementPlan)
        {
          plans.Add(item);
        }
        else if (!showOnlyChildren && !(item.Paths.FullPath.Equals(doNotShowFolder, StringComparison.InvariantCultureIgnoreCase)) && item.HasChildren)
        {
          this.GetAutomationPlans(item, plans);
        }
      }
      //end of the modified part
      return plans;
    }

    private object GetModel(int after, string search)
    {
      if (this.PageSize <= 50)
      {
        this.PageSize = 50;
      }
      if (this.PageIndex < 0)
      {
        this.PageIndex = 0;
      }
      if (after > 0)
      {
        this.PageIndex = (after < this.PageSize) ? 1 : (after / this.PageSize);
      }
      List<Automation> list = new List<Automation>();
      this.Filter(search);
      foreach (Item item in (from i in this.Filter(search)
                             orderby i.DisplayName
                             select i).Skip<Item>((this.PageIndex * this.PageSize)).Take<Item>(this.PageSize))
      {
        List<State> states = this.GetStates(item, search);
        Automation automation = new Automation
        {
          Name = item.DisplayName,
          States = states.ToArray()
        };
        list.Add(automation);
      }
      return list;
    }

    private string GetOptions()
    {
      StringBuilder builder = new StringBuilder();
      builder.Append("{ 'watemark' : ");
      builder.AppendFormat("'{0}',", DependenciesManager.ResourceManager.Localize("ENTER_SEARCH_CRITERIA"));
      builder.AppendFormat("'data' : {0},", JsonConvert.SerializeObject(this.GetModel(0, null)));
      builder.AppendFormat("'loading' : '{0}',", DependenciesManager.ResourceManager.Localize("LOADING"));
      builder.AppendFormat("'nodata' : '{0}'", DependenciesManager.ResourceManager.Localize("THERE_ARE_NO_PLANS"));
      builder.Append("}");
      return builder.ToString();
    }

    private List<State> GetStates(Item item, string search)
    {
      List<State> list = new List<State>();
      if (string.IsNullOrEmpty(search))
      {
        search = string.Empty;
      }

      // modified part to fix issue #171855
      search = search.ToLower();
      //end of the modified part
      foreach (Item item2 in from i in item.Children
                             orderby i.DisplayName
                             select i)
      {
        if (string.IsNullOrEmpty(search) || item2.DisplayName.ToLower().Contains(search))
        {
          State state = new State
          {
            Id = item2.ID.Guid,
            Name = item2.DisplayName
          };
          list.Add(state);
        }
      }
      return list;
    }

    private bool IsChildNameContainsPattern(Item item, string search) =>
        (item.Children.FirstOrDefault<Item>(c => c.DisplayName.ToLower().Contains(search)) != null);

    protected override void OnInit(EventArgs e)
    {
      base.OnInit(e);
      string formValue = WebUtil.GetFormValue("lv-page");
      string search = WebUtil.GetFormValue("lv-search");
      if (!string.IsNullOrEmpty(formValue))
      {
        int num;
        int.TryParse(formValue, out num);
        object model = this.GetModel(num, search);
        this.Page.Response.ReturnOnly(DependenciesManager.ConvertionUtil.ConvertToJson(model));
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);
      if (this.Container.ID == null)
      {
        this.Container.ID = "Container";
      }
      string key = "$sc(document).ready(function(){ $sc('#" + this.Container.ID + "').listview(" + this.GetOptions() + ");});";
      this.Page.ClientScript.RegisterStartupScript(this.Page.GetType(), key, key, true);
      if (!string.IsNullOrEmpty(this.ID))
      {
        key = "$sc(document).ready(function(){$sc('#" + this.Container.ID + "').bind('listview:change', function(e, v){ $sc('#" + this.ID + "_hidden').val(v)});});";
        this.Page.ClientScript.RegisterStartupScript(this.Page.GetType(), key, key, true);
      }
    }

    public override void RenderControl(HtmlTextWriter writer)
    {
      writer.Write("<input id='" + this.ID + "_hidden' type='hidden'/>");
      base.RenderControl(writer);
    }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public string Value =>
        WebUtil.GetFormValue(this.ID + "_hidden");

    private class Automation
    {
      [JsonProperty("n")]
      public string Name { get; set; }

      [JsonProperty("s")]
      public AutomationStateList.State[] States { get; set; }
    }

    private class State
    {
      [JsonProperty("id")]
      public Guid Id { get; set; }

      [JsonProperty("n")]
      public string Name { get; set; }
    }
  }
}
