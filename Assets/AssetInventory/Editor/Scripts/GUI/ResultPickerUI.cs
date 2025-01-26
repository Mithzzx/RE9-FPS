using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetInventory
{
    public sealed class ResultPickerUI : IndexUI
    {
        public static ResultPickerUI Show(Action<string> callback, string searchType = null, string searchPhrase = null)
        {
            ResultPickerUI window = GetWindow<ResultPickerUI>("Asset Inventory");
            window.minSize = new Vector2(650, 300);
            window.searchMode = true;
            window.fixedSearchType = searchType;
            window.instantSelection = true;
            window.hideDetailsPane = true;
            window.disablePings = true;
            window.searchModeCallback = callback;
            window.Show();
            window.SetInitialSearch(searchPhrase);

            return window;
        }
        
        public static ResultPickerUI ShowTextureSelection(Action<Dictionary<string, string>> callback, string searchPhrase = null)
        {
            ResultPickerUI window = GetWindow<ResultPickerUI>("Asset Inventory");
            window.minSize = new Vector2(650, 300);
            window.searchMode = true;
            window.textureMode = true;
            window.fixedSearchType = "Images";
            window.instantSelection = true;
            window.hideDetailsPane = true;
            window.disablePings = true;
            window.searchModeTextureCallback = callback;
            window.Show();
            window.SetInitialSearch(searchPhrase);

            return window;
        }        
    }
}
