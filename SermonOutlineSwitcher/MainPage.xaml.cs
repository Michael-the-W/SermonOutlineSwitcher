using SermonOutlineSwitcher.Classes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using static System.Text.Encoding;
using static Windows.Storage.ApplicationData;

namespace SermonOutlineSwitcher
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string INPUT_FIELD_NAME = "InputFieldName";
        private const string INPUT_NAME = "InputName";
        private const string IP_AND_PORT = "IPandPort";
        public  ObservableCollection<OutlineItem> Outline { get; } = new ObservableCollection<OutlineItem>();
        private readonly List<OutlineItem> _activeList = new List<OutlineItem>();
        private string _inputName;
        private string _inputFieldName;
        private string _internetAddress;
        ApplicationDataContainer localSettings = Current.LocalSettings;

        public MainPage()
        {
            this.InitializeComponent();
            UpdateWebLinkData();
            AssignPageWebData();
        }

        private async void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            DisplayOutline.Text = String.Empty;
            Outline.Clear();
            _activeList.Clear();

            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            openPicker.FileTypeFilter.Add(".txt");

            StorageFile file = await openPicker.PickSingleFileAsync();
            
            if (file != null)
            {
                // Application now has read/write access to the picked file
                FileNameTxt.Text = file.Name;
                IList<string> lines =  await FileIO.ReadLinesAsync(file);
                int order = 0;
                foreach (string line in lines)
                {
                    Outline.Add(new OutlineItem(line, order));
                    order++;
                }

                foreach (OutlineItem item in Outline)
                {
                    item.Active = false;
                }

            }
            else
            {
                FileNameTxt.Text = "File Open Cancelled.";
            }
        }

        private void DisplayBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox tempBox = sender as CheckBox;
            TextBox textbox = (TextBox)tempBox?.FindName("OutlineTxt");
            string line = textbox?.Text;
            TextBlock orderTextBlock = (TextBlock)tempBox?.FindName("Order");
            int lineOrder;
            try
            {
                lineOrder = int.Parse(orderTextBlock.Text);
            }
            catch (NullReferenceException)
            {
                lineOrder = 0;
            }

            if (textbox == null)
            {
                ErrorTxt.Text = "** Nothing found in the Outline Item to put in the outline. **";
                return;
            }
            if (tempBox.IsChecked == true)
            {
                if (_activeList.Count == 0)
                {
                    _activeList.Add(new OutlineItem(line, lineOrder, true));
                }
                else
                {
                    int activeListCount = _activeList.Count;
                    for(int index = 0; index < activeListCount; index++)
                    {
                        int listItemOrder = _activeList[index].Order;
                        // Add to list if the new item comes directly after the current item of the list
                        if(listItemOrder < lineOrder && listItemOrder != lineOrder)
                        {

                            OutlineItem newItem = new OutlineItem(line, lineOrder, true);
                            OutlineItem previousItem = _activeList.Find(listItem => listItem.Order == listItemOrder);
                            if (_activeList.Count == 1)
                            {
                                _activeList.Insert(_activeList.IndexOf(previousItem) + 1, newItem);
                                DisplayOutline.Text = string.Empty;
                                PrintDisplayOutline();
                                return;
                            }

                            if (index + 1 == activeListCount)
                            {
                                _activeList.Insert(_activeList.IndexOf(previousItem) + 1, newItem);
                                DisplayOutline.Text = string.Empty;
                                PrintDisplayOutline();
                                return;
                            }
                        }

                        if (listItemOrder > lineOrder && listItemOrder != lineOrder)
                        {
                            // Get the index of the current item
                            int tempIndex = index;
                            // Get the next item in the list and check if it is null.
                            // If that item is null, add the new item where it is
                            if ((tempIndex + 1) == activeListCount)
                            {
                                _activeList.Add(new OutlineItem(line, lineOrder, true));
                                OutlineItem newItem = _activeList[index + 1];
                                _activeList[index + 1] = _activeList[index];
                                for (int rIndex = 0; rIndex < _activeList.Count; rIndex++)
                                {
                                    if (newItem.Order <= _activeList[rIndex].Order)
                                    {
                                        OutlineItem tempItem = _activeList[rIndex];
                                        _activeList[rIndex] = newItem;
                                        newItem = tempItem;
                                    }
                                }
                                index = activeListCount + 1;
                            }
                        }
                    }
                }

                DisplayOutline.Text = string.Empty;
                PrintDisplayOutline();
                return;
            }
            
            if(tempBox.IsChecked == false)
            {
                int lineIndex = _activeList.FindIndex(item => item.Order == lineOrder);
                _activeList.RemoveAt(lineIndex);
                DisplayOutline.Text = string.Empty;
                PrintDisplayOutline();
            }
        }

        private void PrintDisplayOutline()
        {
            foreach(OutlineItem item in _activeList)
            {
                if (item.Active)
                {
                    DisplayOutline.Text += item.Text + "\n";
                }
            }
        }

        private string VmixFormattedOutline()
        {
            string returnString = string.Empty;
            foreach (OutlineItem item in _activeList)
            {
                if (item.Active)
                {
                    returnString += item.Text + '\n';
                }
            }
            return returnString;
        }

        private async void UpdateVmixInput()
        {
            UpdateWebLinkData();
            string outlineText = VmixFormattedOutline();
            outlineText =  Uri.EscapeUriString(outlineText);
            //Create an HTTP client object
            Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

            //Add a user-agent header to the GET request. 
            var headers = httpClient.DefaultRequestHeaders;

            //The safe way to add a header value is to use the TryParseAdd method and verify the return value is true,
            //especially if the header value is coming from user input.
            string header = "ie";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }

            header = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
            if (!headers.UserAgent.TryParseAdd(header))
            {
                throw new Exception("Invalid header value: " + header);
            }

            Uri requestUri = new Uri(_internetAddress + "/api/?Function=SetText&Input="+ _inputName + "&SelectedName="+ _inputFieldName + "&Value=" + outlineText);

            //Send the GET request asynchronously and retrieve the response as a string.
            Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();

            try
            {
                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponse.EnsureSuccessStatusCode();
                ErrorTxt.Text = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                ErrorTxt.Text = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }

        private void UpdateInput_Click(object sender, RoutedEventArgs e)
        {
            UpdateVmixInput();
        }

        private void InputFieldName_TextChanged(object sender, TextChangedEventArgs e)
        {
            localSettings.Values[INPUT_FIELD_NAME] = InputFieldName.Text;
        }

        private void InputName_TextChanged(object sender, TextChangedEventArgs e)
        {
            localSettings.Values[INPUT_NAME] = InputName.Text;
        }

        private void InternetAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            localSettings.Values[IP_AND_PORT] = InternetAddress.Text;
        }

        // Update the Input Name, Input Field Name, and the IP Address and Port for sending web request
        private void UpdateWebLinkData()
        {
            _inputFieldName = localSettings.Values[INPUT_FIELD_NAME] as string;
            _inputName = localSettings.Values[INPUT_NAME] as string;
            _internetAddress = localSettings.Values[IP_AND_PORT] as string;
        }

        private void AssignPageWebData()
        {
            InputName.Text = _inputName ?? string.Empty;
            InputFieldName.Text = _inputFieldName ?? string.Empty;
            InternetAddress.Text = _internetAddress ?? string.Empty;
        }
    }
}
