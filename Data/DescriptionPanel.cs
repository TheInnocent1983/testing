using Godot;
using System.Collections.Generic;

namespace Parkour.UI.Settings;

public partial class DescriptionPanel : PanelContainer
{
    [ExportGroup("UI Components")]
    [Export] private Label _descriptionTitle;
    [Export] private Label _descriptionBody;

    [ExportGroup("Settings")]
    [Export(PropertyHint.File, "*.json")] 
    private string _jsonPath = "res://Data/descriptions.json";

    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> _descriptionsData;
    
    // Default language ("en" or "ru")
    public string CurrentLanguage { get; set; } = "en"; 

    public override void _Ready()
    {
        LoadDescriptionsFromJson();
        ClearDescription();
    }

    private void LoadDescriptionsFromJson()
    {
        if (!FileAccess.FileExists(_jsonPath))
        {
            GD.PrintErr($"[DescriptionPanel] JSON file not found at: {_jsonPath}");
            return;
        }

        using var file = FileAccess.Open(_jsonPath, FileAccess.ModeFlags.Read);
        var json = new Json();
        
        if (json.Parse(file.GetAsText()) == Error.Ok)
        {
            var rawData = (Godot.Collections.Dictionary)json.Data;
            _descriptionsData = ParseGodotDictionary(rawData);
        }
        else
        {
            GD.PrintErr($"[DescriptionPanel] JSON Parse Error: {json.GetErrorMessage()}");
        }
    }

    /// <summary>
    /// Call this method from ANY setting control to show text by ID
    /// </summary>
    public void ShowDescription(string key)
    {
        if (_descriptionsData == null) return;

        string lang = _descriptionsData.ContainsKey(CurrentLanguage) ? CurrentLanguage : "en";

        if (_descriptionsData.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var entry))
        {
            string titleText = entry.GetValueOrDefault("title", "");
            string bodyText = entry.GetValueOrDefault("body", "");

            if (_descriptionTitle != null)
            {
                _descriptionTitle.Text = titleText;
                // Automatically hide title if empty so layout shrinks vertically
                _descriptionTitle.Visible = !string.IsNullOrWhiteSpace(titleText);
            }

            if (_descriptionBody != null)
            {
                _descriptionBody.Text = bodyText;
            }
        }
    }

    public void ClearDescription()
    {
        ShowDescription("default_hover");
    }

    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> ParseGodotDictionary(Godot.Collections.Dictionary root)
    {
        var result = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

        foreach (var langKey in root.Keys)
        {
            string langStr = langKey.AsString();
            var langDict = (Godot.Collections.Dictionary)root[langKey];
            result[langStr] = new Dictionary<string, Dictionary<string, string>>();

            foreach (var itemKey in langDict.Keys)
            {
                string itemStr = itemKey.AsString();
                var itemDict = (Godot.Collections.Dictionary)langDict[itemKey];

                result[langStr][itemStr] = new Dictionary<string, string>
                {
                    { "title", itemDict.ContainsKey("title") ? itemDict["title"].AsString() : "" },
                    { "body", itemDict.ContainsKey("body") ? itemDict["body"].AsString() : "" }
                };
            }
        }
        return result;
    }
}