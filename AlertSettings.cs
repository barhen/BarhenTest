using PT.Common;
using PT.PackageDefinitions;
using PT.PackageDefinitionsUI.LayoutSetting;
using PT.UIDefinitions.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using DevExpress.Data.Extensions;
using DevExpress.Office.Utils;

using PT.PackageDefinitionsUI.Packages;

using PT.PackageDefinitions.Interfaces;
using PT.PackageDefinitionsUI;
using static DevExpress.CodeParser.CodeStyle.Formatting.Rules;
using PT.Scheduler;

namespace PT.AlertsPackage.ControlSettings
{
    public class AlertSettingsCollection : ISettingData, IEquatable<AlertSettingsCollection>
    {
        private readonly Dictionary<string, List<(string, AlertSettings)>> m_settingsDictionary;

        public Dictionary<string, List<(string, AlertSettings)>> SettingsDictionary
        {
            get { return m_settingsDictionary; }
        }

        public AlertSettingsCollection(IReader a_reader)
        {
            if (a_reader.VersionNumber >= 12000)
            {
                a_reader.Read(out int grids);

                m_settingsDictionary = new Dictionary<string, List<(string, AlertSettings)>>();

                for (int iKey = 0; iKey < grids; iKey++)
                {
                    a_reader.Read(out string gridName);

                    List<(string, AlertSettings)> alertList = new List<(string, AlertSettings)>();

                    a_reader.Read(out int alerts);
                    for (int i = 0; i < alerts; i++)
                    {
                        a_reader.Read(out string layoutName);

                        AlertSettings setting = new AlertSettings(a_reader);
                        alertList.Add((layoutName, setting));
                    }

                    m_settingsDictionary[gridName] = alertList;
                }
            }
            else if (a_reader.VersionNumber >= 1)
            {
                a_reader.Read(out int grids);

                m_settingsDictionary = new Dictionary<string, List<(string, AlertSettings)>>();

                for (int iKey = 0; iKey < grids; iKey++)
                {
                    a_reader.Read(out string gridName);

                    List<(string, AlertSettings)> alertList = new List<(string, AlertSettings)>();

                    a_reader.Read(out int alerts);
                    for (int i = 0; i < alerts; i++)
                    {
                        a_reader.Read(out string layoutName);
                        a_reader.Read(out string gridFilter);
                        a_reader.Read(out string dataTypeKey);
                        a_reader.Read(out string paneKey);

                        AlertSettings setting = new AlertSettings(gridFilter, dataTypeKey, paneKey);
                        alertList.Add((layoutName, setting));
                    }

                    m_settingsDictionary[gridName] = alertList;
                }
            }
        }

        internal AlertSettingsCollection()
        {
            m_settingsDictionary = new Dictionary<string, List<(string, AlertSettings)>>();
        }

        public void Serialize(IWriter a_writer)
        {
            a_writer.Write(m_settingsDictionary.Count);

            foreach (KeyValuePair<string, List<(string, AlertSettings)>> pair in m_settingsDictionary)
            {
                a_writer.Write(pair.Key);
                a_writer.Write(pair.Value.Count);
                foreach ((string LayoutName, AlertSettings Alert) alertSetting in pair.Value)
                {
                    a_writer.Write(alertSetting.LayoutName);
                    alertSetting.Alert.Serialize(a_writer);
                }
            }
        }

        public int UniqueId => 1023;
        public string SettingKey => "workspace_AlertSettingsCollection";
        public string Description => "Metric Settings";
        public string SettingsGroup => SettingGroupConstants.MetricsSettingsGroup;
        public string SettingsGroupCategory => SettingGroupConstants.MetricCollection;
        public string SettingCaption => "Metrics";


        public void SaveAlert(string a_gridKey, string a_activeFilterString, string a_dataTypeKey, string a_layoutName, string a_paneKey, LayoutSetting a_activeLayout)
        {
            (string, AlertSettings) newLayoutAlert = (a_layoutName, new AlertSettings(a_activeFilterString, a_dataTypeKey, a_paneKey, a_activeLayout));

            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string LayoutName, AlertSettings AlertSetting)> alerts))
            {
                DeleteLayoutAlert(a_layoutName, alerts);
                alerts.Add(newLayoutAlert);
            }
            else
            {
                List<(string, AlertSettings)> newAlerts = new List<(string, AlertSettings)>();
                newAlerts.Add(newLayoutAlert);
                SettingsDictionary.Add(a_gridKey, newAlerts);
            }
        }

        private bool DeleteLayoutAlert(string a_layoutName, List<(string LayoutName, AlertSettings AlertSetting)> alerts)
        {
            List<(string, AlertSettings)> layoutAlerts = alerts.FindAll(a_alert => a_alert.LayoutName == a_layoutName);

            if (layoutAlerts.Count > 0)
            {
                foreach ((string, AlertSettings) layout in layoutAlerts)
                {
                    alerts.Remove(layout);
                }

                return true;
            }

            return false;
        }

        public bool DeleteAlert(string a_gridKey, string a_layoutName)
        {
            bool deleted = false;
            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string, AlertSettings)> alerts))
            {
                deleted = DeleteLayoutAlert(a_layoutName, alerts);

                if (alerts.Count == 0)
                {
                    SettingsDictionary.Remove(a_gridKey);
                }
            }

            return deleted;
        }

        public void UpdateAlertSettings(string a_gridKey, string a_layoutName, (UIEnums.EAlertPriority Priority, int PriorityValue, Color PriorityColor, bool HigherIsBetter, bool FilterHighlightIsOn, bool ShowTargetIsOn, int TargetValue, bool TargetTypeIsNumber, decimal TargetTolerance, bool ShowFilterValueAsPercent) a_priority)
        {
            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string LayoutName, AlertSettings AlertSetting)> alerts))
            {
                foreach ((string LayoutName, AlertSettings AlertSetting) alert in alerts)
                {
                    if (alert.LayoutName == a_layoutName)
                    {
                        alert.AlertSetting.Priority = (UIEnums.EAlertPriority)Enum.ToObject(typeof(UIEnums.EAlertPriority), a_priority.Priority);
                        alert.AlertSetting.PriorityColor = a_priority.PriorityColor;
                        alert.AlertSetting.PriorityValue = a_priority.PriorityValue;
                        alert.AlertSetting.HigherIsBetter = a_priority.HigherIsBetter;
                        alert.AlertSetting.ShowFilterHighlightIsOn = a_priority.FilterHighlightIsOn;
                        alert.AlertSetting.ShowTargetIsOn = a_priority.ShowTargetIsOn;
                        alert.AlertSetting.TargetValue = a_priority.TargetValue;
                        alert.AlertSetting.TargetTypeIsNumber = a_priority.TargetTypeIsNumber;
                        alert.AlertSetting.TargetTolerance = a_priority.TargetTolerance;
                        alert.AlertSetting.ShowFilterValueAsPercent = a_priority.ShowFilterValueAsPercent;
                    }
                }
            }
        }

        public bool ValidateAlerts(string a_gridKey, List<LayoutSetting> a_gridLayouts, LayoutSetting a_layoutSetting)
        {
            bool alertChanged = false;
            List<LayoutSetting> alertsToAdd = new List<LayoutSetting>();
            List<string> alertsToRemove = new List<string>();

            //Validate current layout
            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string LayoutName, AlertSettings AlertSetting)> currentAlerts))
            {
                if (currentAlerts.Any(alert => alert.LayoutName == a_layoutSetting.LayoutName))
                {
                    (string LayoutName, AlertSettings AlertSetting) layoutAlert = currentAlerts.Find(alert => alert.LayoutName == a_layoutSetting.LayoutName);

                    if (layoutAlert.AlertSetting.CustomColorIsOn != a_layoutSetting.CustomColorIsOn)
                    {
                        layoutAlert.AlertSetting.CustomColorIsOn = a_layoutSetting.CustomColorIsOn;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.GridFilter != a_layoutSetting.ActiveFilterString)
                    {
                        layoutAlert.AlertSetting.GridFilter = a_layoutSetting.ActiveFilterString;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.CustomSummary.Equals(a_layoutSetting.CustomSummary))
                    {
                        layoutAlert.AlertSetting.CustomSummary = a_layoutSetting.CustomSummary;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.PriorityColor != a_layoutSetting.PriorityColor)
                    {
                        layoutAlert.AlertSetting.PriorityColor = a_layoutSetting.PriorityColor;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.PriorityValue != a_layoutSetting.PriorityValue)
                    {
                        layoutAlert.AlertSetting.PriorityValue = a_layoutSetting.PriorityValue;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.HigherIsBetter != a_layoutSetting.HigherIsBetter)
                    {
                        layoutAlert.AlertSetting.HigherIsBetter = a_layoutSetting.HigherIsBetter;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.ShowFilterHighlightIsOn != a_layoutSetting.ShowFilterHighlightIsOn)
                    {
                        layoutAlert.AlertSetting.ShowFilterHighlightIsOn = a_layoutSetting.ShowFilterHighlightIsOn;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.ShowTargetIsOn != a_layoutSetting.ShowTargetIsOn)
                    {
                        layoutAlert.AlertSetting.ShowTargetIsOn = a_layoutSetting.ShowTargetIsOn;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.TargetValue != a_layoutSetting.TargetValue)
                    {
                        layoutAlert.AlertSetting.TargetValue = a_layoutSetting.TargetValue;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.TargetTypeIsNumber != a_layoutSetting.TargetTypeIsNumber)
                    {
                        layoutAlert.AlertSetting.TargetTypeIsNumber = a_layoutSetting.TargetTypeIsNumber;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.TargetTolerance != a_layoutSetting.TargetTolerance)
                    {
                        layoutAlert.AlertSetting.TargetTolerance = a_layoutSetting.TargetTolerance;
                        alertChanged = true;
                    }

                    if (layoutAlert.AlertSetting.ShowFilterValueAsPercent != a_layoutSetting.ShowFilterValueAsPercent)
                    {
                        layoutAlert.AlertSetting.ShowFilterValueAsPercent = a_layoutSetting.ShowFilterValueAsPercent;
                        alertChanged = true;
                    }
                }
                else
                {
                    if (a_layoutSetting.MetricsEnabledIsOn)
                    {
                        alertsToAdd.Add(a_layoutSetting);
                        alertChanged = true;
                    }
                    else
                    {
                        alertsToRemove.Add(a_layoutSetting.LayoutName);
                        alertChanged = true;
                    }
                }
            }
            else
            {
                if (a_layoutSetting.MetricsEnabledIsOn)
                {
                    alertsToAdd.Add(a_layoutSetting);
                    alertChanged = true;
                }
                else
                {
                    alertsToRemove.Add(a_layoutSetting.LayoutName);
                    alertChanged = true;
                }
            }

            //Validate all other alerts/layouts match
            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string LayoutName, AlertSettings AlertSetting)> alerts))
            {
                if (!alerts.All(alert => a_gridLayouts.Any(layout => layout.LayoutName == alert.LayoutName && layout.MetricsEnabledIsOn && layout.LayoutName != a_layoutSetting.LayoutName)) || !a_gridLayouts.All(layout => alerts.Any(alert => alert.LayoutName == layout.LayoutName && layout.MetricsEnabledIsOn && layout.LayoutName != a_layoutSetting.LayoutName)))
                {
                    //Validate layouts
                    foreach (LayoutSetting layoutSetting in a_gridLayouts)
                    {
                        if (layoutSetting.LayoutName != a_layoutSetting.LayoutName)
                        {
                            if (layoutSetting.MetricsEnabledIsOn)
                            {
                                if (alerts.All(alert => alert.LayoutName != layoutSetting.LayoutName))
                                {
                                    alertsToAdd.Add(a_layoutSetting);
                                    alertChanged = true;
                                }
                            }
                            else
                            {
                                if (alerts.Any(alert => alert.LayoutName == layoutSetting.LayoutName))
                                {
                                    alertsToRemove.Add(a_layoutSetting.LayoutName);
                                    alertChanged = true;
                                }
                            }
                        }
                    }

                    //Validate alerts
                    foreach ((string LayoutName, AlertSettings AlertSetting) alert in alerts)
                    {
                        if (alert.LayoutName != a_layoutSetting.LayoutName)
                        {
                            if (a_gridLayouts.All(layout => layout.LayoutName != alert.LayoutName))
                            {
                                alertsToRemove.Add(alert.LayoutName);
                                alertChanged = true;
                            }
                        }
                    }
                }
            }

            foreach (LayoutSetting layoutSetting in alertsToAdd)
            {
                CreateNewAlert(a_gridKey, layoutSetting);
            }

            foreach (string layoutName in alertsToRemove)
            {
                DeleteAlert(a_gridKey, layoutName);
            }

            return alertChanged;
        }

        private void CreateNewAlert(string a_gridKey, LayoutSetting a_layoutSetting)
        {
            string dataTypeKey = GridDataTypes.GetGridDataType(a_gridKey);
            SaveAlert(a_gridKey, a_layoutSetting.ActiveFilterString, dataTypeKey, a_layoutSetting.LayoutName, BoardKeys.GetBoardKey(dataTypeKey), a_layoutSetting);
        }
        /// <summary>
        /// Gets an alertSettings matching the parameter provided
        /// </summary>
        /// <param name="a_gridKey"></param>
        /// <param name="a_layoutName"></param>
        /// <returns></returns>
        public AlertSettings GetAlertSettings(string a_gridKey, string a_layoutName)
        {
            if (SettingsDictionary.TryGetValue(a_gridKey, out List<(string LayoutName, AlertSettings AlertSetting)> alerts))
            {
                return alerts.FirstOrDefault(k => k.LayoutName == a_layoutName).AlertSetting;
            }

            return null;
        }
        public bool Equals(AlertSettingsCollection a_other)
        {
            if (a_other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, a_other))
            {
                return true;
            }
            foreach ((string key, List<(string, AlertSettings)> value) in a_other.SettingsDictionary)
            {
                if(m_settingsDictionary.TryGetValue(key, out List<(string, AlertSettings)> testValue))
                {
                    if (!testValue.Equals(value)) return false;

                    //foreach ((string, AlertSettings) valueTuple in testValue)
                    //{
                    //    if (!valueTuple.Item2.Equals(value))
                    //        return false;
                    //}
                }
                //foreach (var valueTuple in value)
                //{
                    
                //}
                //if (!a_other.SettingsDictionary.ContainsValue(value.))
                //{
                //    return false;
                //}
            }

            return true;
        }

        public override bool Equals(object a_obj)
        {
            if (null == a_obj)
            {
                return false;
            }

            if (ReferenceEquals(this, a_obj))
            {
                return true;
            }

            if (a_obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((AlertSettingsCollection)a_obj);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(m_settingsDictionary);
            return hashCode.ToHashCode();
        }
    }

    public class AlertSettings : ISettingData, IEquatable<AlertSettings>
    {
        private IDynamicSkin m_theme;
        public AlertSettings(IReader a_reader)
        {
            if (a_reader.VersionNumber >= 12000)
            {
                a_reader.Read(out m_gridFilter);
                a_reader.Read(out m_dataTypeKey);
                a_reader.Read(out m_paneKey);
                int val;
                a_reader.Read(out val);
                Priority = (UIEnums.EAlertPriority)val;
                a_reader.Read(out m_priorityColor);
                a_reader.Read(out m_priorityValue);
                a_reader.Read(out m_higherIsBetter);
                a_reader.Read(out m_showFilterHighlightIsOn);
                m_customSummary = new SummarySettings(a_reader);
                a_reader.Read(out m_showTargetIsOn);
                a_reader.Read(out m_targetValue);
                a_reader.Read(out m_targetTypeIsNumber);
                a_reader.Read(out m_targetTolerance);
                a_reader.Read(out m_showFilterValueAsPercent);
                a_reader.Read(out m_customColorIsOn);
            }
            else if (a_reader.VersionNumber >= 1)
            {
                a_reader.Read(out m_gridFilter);
                a_reader.Read(out m_dataTypeKey);
                a_reader.Read(out m_paneKey);
            }
        }

        internal AlertSettings(IDynamicSkin a_theme)
        {
            m_theme = a_theme;
            m_gridFilter = "";
            m_dataTypeKey = "";
            m_paneKey = "";
            Priority = UIEnums.EAlertPriority.None;
            m_priorityColor = a_theme.PriorityColorDefault;
            m_priorityValue = (int)UIEnums.EAlertPriority.None;
            HigherIsBetter = true;
            CustomSummary = new SummarySettings();
            m_showFilterHighlightIsOn = true;
            m_showTargetIsOn = false;
            m_targetValue = 0;
            m_targetTypeIsNumber = true;
            m_targetTolerance = 0;
            m_showFilterValueAsPercent = false;
            m_customColorIsOn = false;
        }

        public AlertSettings(string a_activeFilterString, string a_dataTypeKeyKey, string a_paneKey)
        {
            m_gridFilter = a_activeFilterString;
            m_dataTypeKey = a_dataTypeKeyKey;
            m_paneKey = a_paneKey;
            Priority = UIEnums.EAlertPriority.None;
            m_priorityColor = m_theme.PriorityColorDefault;
            m_priorityValue = (int)UIEnums.EAlertPriority.None;
            HigherIsBetter = true;
            CustomSummary = new SummarySettings();
            m_showFilterHighlightIsOn = true;
            m_showTargetIsOn = false;
            m_targetValue = 0;
            m_targetTypeIsNumber = true;
            m_targetTolerance = 0;
            m_showFilterValueAsPercent = false;
            m_customColorIsOn = false;
        }

        public AlertSettings(string a_activeFilterString, string a_dataTypeKeyKey, string a_paneKey, LayoutSetting a_activeLayout)
        {
            m_gridFilter = a_activeFilterString;
            m_dataTypeKey = a_dataTypeKeyKey;
            m_paneKey = a_paneKey;
            Priority = a_activeLayout.Priority;
            m_priorityColor = a_activeLayout.PriorityColor;
            m_priorityValue = a_activeLayout.PriorityValue;
            HigherIsBetter = a_activeLayout.HigherIsBetter;
            CustomSummary = a_activeLayout.CustomSummary;
            m_showFilterHighlightIsOn = a_activeLayout.ShowFilterHighlightIsOn;
            m_showTargetIsOn = a_activeLayout.ShowTargetIsOn;
            m_targetValue = a_activeLayout.TargetValue;
            m_targetTypeIsNumber = a_activeLayout.TargetTypeIsNumber;
            m_targetTolerance = a_activeLayout.TargetTolerance;
            m_showFilterValueAsPercent = a_activeLayout.ShowFilterValueAsPercent;
            m_customColorIsOn = a_activeLayout.CustomColorIsOn;
        }

        public void Serialize(IWriter a_writer)
        {
            a_writer.Write(GridFilter);
            a_writer.Write(DataTypeKey);
            a_writer.Write(PaneKey);
            a_writer.Write((int)Priority);
            a_writer.Write(PriorityColor);
            a_writer.Write(PriorityValue);
            a_writer.Write(HigherIsBetter);
            a_writer.Write(ShowFilterHighlightIsOn);
            CustomSummary.Serialize(a_writer);
            a_writer.Write(ShowTargetIsOn);
            a_writer.Write(TargetValue);
            a_writer.Write(TargetTypeIsNumber);
            a_writer.Write(TargetTolerance);
            a_writer.Write(ShowFilterValueAsPercent);
            a_writer.Write(CustomColorIsOn);
        }

        private string m_gridFilter;
        public string GridFilter
        {
            set { m_gridFilter = value; }
            get { return m_gridFilter; }
        }

        private readonly string m_dataTypeKey;
        public string DataTypeKey
        {
            get { return m_dataTypeKey; }
        }

        private readonly string m_paneKey;
        public string PaneKey
        {
            get { return m_paneKey; }
        }

        private SummarySettings m_customSummary;
        public SummarySettings CustomSummary
        {
            get { return m_customSummary; }
            set { m_customSummary = value; }
        }

        public UIEnums.EAlertPriority Priority { get; set; }

        private int m_priorityValue;
        public int PriorityValue
        {
            get { return m_priorityValue; }
            set { m_priorityValue = value; }
        }

        private Color m_priorityColor;
        public Color PriorityColor
        {
            get { return m_priorityColor; }
            set { m_priorityColor = value; }
        }

        private bool m_higherIsBetter;
        public bool HigherIsBetter
        {
            get { return m_higherIsBetter; }
            set { m_higherIsBetter = value; }
        }

        private bool m_showFilterHighlightIsOn;
        public bool ShowFilterHighlightIsOn
        {
            get { return m_showFilterHighlightIsOn; }
            set { m_showFilterHighlightIsOn = value; }
        }

        private bool m_showTargetIsOn;
        public bool ShowTargetIsOn
        {
            get { return m_showTargetIsOn; }
            set { m_showTargetIsOn = value; }
        }

        private decimal m_targetValue;
        public decimal TargetValue
        {
            get { return m_targetValue; }
            set { m_targetValue = value; }
        }

        private bool m_targetTypeIsNumber;
        public bool TargetTypeIsNumber
        {
            get { return m_targetTypeIsNumber; }
            set { m_targetTypeIsNumber = value; }
        }

        private bool m_showFilterValueAsPercent;
        public bool ShowFilterValueAsPercent
        {
            get { return m_showFilterValueAsPercent; }
            set { m_showFilterValueAsPercent = value; }
        }

        private decimal m_targetTolerance;
        public decimal TargetTolerance
        {
            get { return m_targetTolerance; }
            set { m_targetTolerance = value; }
        }

        private bool m_customColorIsOn;
        public bool CustomColorIsOn
        {
            get { return m_customColorIsOn; }
            set { m_customColorIsOn = value; }
        }

        public int UniqueId => 1022;
        public string SettingKey => "workspace_AlertSetting";
        public string Description => "Metric Settings";
        public string SettingsGroup => SettingGroupConstants.MetricsSettingsGroup;
        public string SettingsGroupCategory => SettingGroupConstants.MetricsSettings;
        public string SettingCaption => "Metric";

        public bool Equals(AlertSettings a_other)
        {
            if (a_other == null)
            {
                return false;
            }

            if (ReferenceEquals(this, a_other))
            {
                return true;
            }

            return
                   m_gridFilter == a_other.m_gridFilter &&
                   m_dataTypeKey == a_other.m_dataTypeKey &&
                   m_paneKey == a_other.m_paneKey &&
                   m_customSummary.Equals(a_other.m_customSummary) &&
                   m_priorityValue == a_other.m_priorityValue &&
                   m_priorityColor.ToArgb().Equals(a_other.m_priorityColor.ToArgb()) &&
                   m_higherIsBetter == a_other.m_higherIsBetter &&
                   m_showFilterHighlightIsOn == a_other.m_showFilterHighlightIsOn &&
                   m_showTargetIsOn == a_other.m_showTargetIsOn &&
                   m_targetValue == a_other.m_targetValue &&
                   m_targetTypeIsNumber == a_other.m_targetTypeIsNumber &&
                   m_showFilterValueAsPercent == a_other.m_showFilterValueAsPercent &&
                   m_targetTolerance == a_other.m_targetTolerance &&
                   m_customColorIsOn == a_other.m_customColorIsOn &&
                   Priority == a_other.Priority;
        }

        public override bool Equals(object a_obj)
        {
            if (null == a_obj)
            {
                return false;
            }

            if (ReferenceEquals(this, a_obj))
            {
                return true;
            }

            if (a_obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((AlertSettings)a_obj);
        }

        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();
            hashCode.Add(m_gridFilter);
            hashCode.Add(m_dataTypeKey);
            hashCode.Add(m_paneKey);
            hashCode.Add(m_customSummary);
            hashCode.Add(m_priorityValue);
            hashCode.Add(m_priorityColor);
            hashCode.Add(m_higherIsBetter);
            hashCode.Add(m_showFilterHighlightIsOn);
            hashCode.Add(m_showTargetIsOn);
            hashCode.Add(m_targetValue);
            hashCode.Add(m_targetTypeIsNumber);
            hashCode.Add(m_showFilterValueAsPercent);
            hashCode.Add(m_targetTolerance);
            hashCode.Add(m_customColorIsOn);
            hashCode.Add((int)Priority);
            return hashCode.ToHashCode();
        }
    }
}
