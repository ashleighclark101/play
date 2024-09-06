﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TBChestTracker.Managers;

namespace TBChestTracker.Pages.AbsentClanmates
{
    /// <summary>
    /// Interaction logic for ProcessingPage.xaml
    /// </summary>
    public partial class ProcessingPage : Page
    {
        private bool bErrorOccured = false;

        private CancellationTokenSource tokenSource { get; set; }

        private bool bIsCanceled { get; set; }

        public ProcessingPage()
        {
            InitializeComponent();
          
        }

        private async Task<ObservableCollection<string>> FindAbsentClanmates()
        {
            ObservableCollection<string> absentClanmates = new ObservableCollection<string>();

            var dailyChestData = ClanManager.Instance.ClanChestManager.ClanChestDailyData;
            var absentCount = 0;
            var daysToSubtract = 0;

            var firstDateKey = dailyChestData.First();
            var lastDateKey = dailyChestData.Last();
            var lastDate = DateTime.Parse(lastDateKey.Key);
            var firstDate = DateTime.Parse(firstDateKey.Key);
            TimeSpan dateDifference = lastDate - firstDate;

            bool bIsValid = false;

            if (AbsentClanmatesViewModel.Instance.AbsentDuration == "A Week")
            {
                daysToSubtract = 7;
                if (dateDifference.Days >= daysToSubtract)
                {
                    bIsValid = true;
                }
            }
            else if (AbsentClanmatesViewModel.Instance.AbsentDuration == "Two Weeks")
            {
                daysToSubtract = 14;
                if (dateDifference.Days >= daysToSubtract)
                {
                    bIsValid = true;
                }
            }
            else if (AbsentClanmatesViewModel.Instance.AbsentDuration == "A Month")
            {
                daysToSubtract = 30;
                if (dateDifference.Days >= daysToSubtract)
                {
                    bIsValid = true;
                }
            }

            if (!bIsValid)
            {
                bErrorOccured = true;
                return null;
            }

            var DayCount = 0;

            //-- NaughtyList[Clanmate] = daysAbsent.
            Dictionary<string, int> naughtyList = new Dictionary<string, int>();

            //-- quick init
            var clanmates = ClanManager.Instance.ClanmateManager.Database.Clanmates;
            AbsentClanmatesViewModel.Instance.MaxProcessingProgress = clanmates.Count;

            foreach (var clanmate in clanmates)
            {
                naughtyList[clanmate.Name] = 0;
            }

            for (var d = 0; d < dailyChestData.Count; d++)
            {
                try
                {
                    var date = DateTime.Now.AddDays(-d).ToShortDateString();
                    //-- will cause exception if user did not count chests

                    var clanchestdata = dailyChestData[date];
                    if (clanchestdata != null)
                    {
                        foreach (var chestdata in clanchestdata)
                        {
                            //--- we iterate through checking chests to be null. 
                            //--- if clanmate exists we count. If not, they could've recently joined.
                            bool bClanmateExists = naughtyList.ContainsKey(chestdata.Clanmate);

                            if (bClanmateExists)
                            {
                                if (chestdata.chests == null)
                                {
                                    naughtyList[chestdata.Clanmate] += 1;
                                }
                                else
                                {
                                    var currentValue = naughtyList[chestdata.Clanmate];
                                    if (currentValue > 0)
                                    {
                                        naughtyList[chestdata.Clanmate] = 0; // reset it. 
                                    }
                                }
                            }
                        }
                        DayCount++;
                    }
                }
                catch (Exception e)
                {

                }
            }

            //-- now we go through the naughtylist and check to see whomever is above 0.

            ProcessingProgressBar.IsIndeterminate = false;
            foreach (var naughty in naughtyList)
            {
                var absentDays = naughty.Value;
                if (absentDays >= daysToSubtract)
                {
                    absentClanmates.Add(naughty.Key);
                }
                AbsentClanmatesViewModel.Instance.ProcessedText = $"Processed {AbsentClanmatesViewModel.Instance.ProcessingProgressValue} out of {AbsentClanmatesViewModel.Instance.MaxProcessingProgress} clanmates.";
                AbsentClanmatesViewModel.Instance.ProcessingProgressValue += 1;
                await Task.Delay(100);
            }

            AbsentClanmatesViewModel.Instance.ProcessingProgressValue = AbsentClanmatesViewModel.Instance.MaxProcessingProgress;


            return absentClanmates;
        }

        private async Task FindAbsentClanmatesTask()
        {
            bErrorOccured = false;

            AbsentClanmatesViewModel.Instance.AbsentClanmateList = await FindAbsentClanmates();
            Debug.WriteLine("I AM DOING A TASK!!!!");

            var wnd = Window.GetWindow(this) as AbsentClanmatesWindow;
            if(AbsentClanmatesViewModel.Instance.AbsentClanmateList == null)
            {
                wnd.NavigateTo("Pages/AbsentClanmates/ErrorPage.xaml");
            }
            else
            {
                wnd.NavigateTo("Pages/AbsentClanmates/ResultsPage.xaml");
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = AbsentClanmatesViewModel.Instance;
            FindAbsentClanmatesTask();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            bErrorOccured = false;
        }
    }
}