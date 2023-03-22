using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EleWise.ELMA.Model.Common;
using EleWise.ELMA.Model.Entities;
using EleWise.ELMA.Model.Managers;
using EleWise.ELMA.Model.Types.Settings;
using EleWise.ELMA.UI.Controllers;
using EleWise.ELMA.UI.Models;
using EleWise.ELMA.API;
using EleWise.ELMA.UI.Results;
using Aspose.Words;

namespace EleWise.ELMA.UI.Pages
{
	/// <summary>
	/// Контроллер страницы "История доработок процессов"
	/// </summary>
	public partial class ProcessDevelopmentHistoryController : PageController<ProcessDevelopmentHistory.Index>
	{
		/// <summary>
		/// Загрузка страницы
		/// </summary>
		public override void Index_Load (PageLoadViewModel<ProcessDevelopmentHistory.Index> page)
		{
			FindByProccVersion_OnChange(page);
		}

		/// <summary>
		/// BuildVersionHistory
		/// </summary>
		/// <param name="page"></param>
		public virtual void BuildVersionHistory (PageViewModel<ProcessDevelopmentHistory.Index> page)
		{
			page.Context.DevelopmentHistory.Clear ();
			if (page.Context.ProcessHeader == null) {
				page.Form.Notifier.Error ("Не выбран процесс!");
				return;
			}
			if(page.Context.ProcessVersion == null)
			{
				//Выборка всех версий по WorkflowProcess
				if (page.Context.ShowVersionsWithoutRequest == true) {
					var workflowProcesses = PublicAPI.Processes.WorkflowProcess.Find (string.Format ("Header = {0}", page.Context.ProcessHeader.Id)).OrderByDescending (X => X.VersionNumber);
					foreach (var workflowProcess in workflowProcesses) {
						var versionBlock = new ProcessDevelopmentHistory.Index_DevelopmentHistory ();
						versionBlock.ProcessVersion = workflowProcess;
						//Поиск записи по соответсвию - Реальная версия процесса == Номер версии процесса в процессе по процессам
						var DevelopmentDictionary = PublicAPI.Objects.UserObjects.UserProcessDevelopment.Find (string.Format ("ProcessHeader = {0} and ProcessVersion = {1}", workflowProcess.Header.Id, workflowProcess.VersionNumber)).FirstOrDefault ();
						if (DevelopmentDictionary != null) {
							versionBlock.DevelopmentDictionaryItem = DevelopmentDictionary;
						}
						page.Context.DevelopmentHistory.Add (versionBlock);
					}
				}
				//Выборка всех версий по справочнику
				else
					if (page.Context.ShowVersionsWithoutRequest == false) {
						var DevDictionary = PublicAPI.Objects.UserObjects.UserProcessDevelopment.Find (string.Format ("ProcessHeader = {0}", page.Context.ProcessHeader.Id));
						foreach (var dictElem in DevDictionary) {
							//Находим реальную версию из дизайнера ELMA
							var realVersion = PublicAPI.Processes.WorkflowProcess.Find (string.Format ("VersionNumber = {0}", dictElem.ProcessVersion)).FirstOrDefault ();
							if (realVersion != null) {
								var versionBlock = new ProcessDevelopmentHistory.Index_DevelopmentHistory ();
								versionBlock.ProcessVersion = realVersion;
								versionBlock.DevelopmentDictionaryItem = dictElem;
								page.Context.DevelopmentHistory.Add (versionBlock);
							}
						}
					}
				//Если не выбрана конкретная версия грузим по дефолту
			} else
			{
				//Если выбрана какая-то версия, грузим её
				var workflowProcess = PublicAPI.Processes.WorkflowProcess.Find(string.Format("Header = {0} and VersionNumber = {1}", page.Context.ProcessHeader.Id, Int32.Parse(page.Context.ProcessVersion.Key))).FirstOrDefault();
				var versionBlock = new ProcessDevelopmentHistory.Index_DevelopmentHistory ();
				versionBlock.ProcessVersion = workflowProcess;
				var DevelopmentDictionary = PublicAPI.Objects.UserObjects.UserProcessDevelopment.Find (string.Format ("ProcessHeader = {0} and ProcessVersion = {1}", workflowProcess.Header.Id, workflowProcess.VersionNumber)).FirstOrDefault ();
				if (DevelopmentDictionary != null) {
					versionBlock.DevelopmentDictionaryItem = DevelopmentDictionary;
				}
				page.Context.DevelopmentHistory.Add (versionBlock);
			}
		}

		/// <summary>
		/// ProcessHeaderOnChange
		/// </summary>
		/// <param name="page"></param>
		public virtual void ProcessHeaderOnChange (PageViewModel<ProcessDevelopmentHistory.Index> page)
		{
			var processVersionDropDown = (DropDownListSettings)page.Context.GetSettingsFor (c => c.ProcessVersion);
			processVersionDropDown.Items.Clear ();
			processVersionDropDown.Save ();
			if (page.Context.ProcessHeader != null && page.Context.ShowProccVersionOnForm == true) {
				if (page.Context.ShowVersionsWithoutRequest == true) {
					var workflowProcesses = PublicAPI.Processes.WorkflowProcess.Find (string.Format ("Header = {0}", page.Context.ProcessHeader.Id)).OrderByDescending (X => X.VersionNumber);
					foreach (var workflowProcess in workflowProcesses) {
						var DevelopmentDictionary = PublicAPI.Objects.UserObjects.UserProcessDevelopment.Find (string.Format ("ProcessHeader = {0} and ProcessVersion = {1}", workflowProcess.Header.Id, workflowProcess.VersionNumber)).FirstOrDefault ();
						var dropDownItem = new DropDownItem ();
						dropDownItem.Key = workflowProcess.VersionNumber.ToString ();
						if (DevelopmentDictionary != null) {
							dropDownItem.Value = string.Format ("Версия {0} по заявке на доработку", workflowProcess.VersionNumber);
						}
						else {
							dropDownItem.Value = string.Format ("Версия {0}", workflowProcess.VersionNumber);
						}
						processVersionDropDown.Items.Add (dropDownItem);
						processVersionDropDown.Save ();
					}
				}
				else {
					var DevDictionary = PublicAPI.Objects.UserObjects.UserProcessDevelopment.Find (string.Format ("ProcessHeader = {0}", page.Context.ProcessHeader.Id));
					foreach (var dictElem in DevDictionary) {
						//Находим реальную версию из дизайнера ELMA
						var realVersion = PublicAPI.Processes.WorkflowProcess.Find (string.Format ("VersionNumber = {0}", dictElem.ProcessVersion)).FirstOrDefault ();
						if (realVersion != null) {
							var dropDownItem = new DropDownItem ();
							dropDownItem.Key = realVersion.VersionNumber.ToString ();
							dropDownItem.Value = string.Format ("Версия {0} с заявкой на доработку", realVersion.VersionNumber);
							processVersionDropDown.Items.Add (dropDownItem);
							processVersionDropDown.Save ();
						}
					}
				}
			}
			else {
				return;
			}
		}

		/// <summary>
		/// FindByProccVersion_OnChange
		/// </summary>
		/// <param name="page"></param>
		public virtual void FindByProccVersion_OnChange (PageViewModel<ProcessDevelopmentHistory.Index> page)
		{
			if(page.Context.ShowProccVersionOnForm == true)
			{
				page.Form.For(f => f.ProcessVersion).Visible(true).Required(true).ReadOnly(false);
				ProcessHeaderOnChange(page);
			} else
			{
				page.Form.For(f => f.ProcessVersion).Visible(false).Required(false).ReadOnly(true);
				page.Context.ProcessVersion = null;
			}
		}
	}
}
