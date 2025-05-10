using Streamer.bot.Plugin.Interface;
using System;

public class CPHInline: CPHInlineBase
{
    public bool AddTimeToTimer()
    {
        if(!CPH.TryGetArg<int>("secondsPerDollar", out int secondsPerDollar))
        {
            secondsPerDollar = 30;
        }

        if (CPH.TryGetArg<int>("tipAmount", out int donationInDollar))
        {
            int newTime = (donationInDollar * secondsPerDollar);
            CPH.LogDebug($"Calculated additional time for Donation of ${donationInDollar}");
            CPH.LogDebug($"Setting timeToAdd to {newTime} now");
            CPH.SetGlobalVar("timeToAdd", newTime, true);
        }
        return true;
    }

    public bool ShowCounter()
    {
        int timeInSeconds = CPH.GetGlobalVar<int>("timeInSeconds", true);
        if(timeInSeconds > 0)
        {
            ShowCountdown(timeInSeconds);
        } else
        {
            ShowCountdown(300);
        }
            
        return true;
    }

    private void ShowCountdown(int countdownInSeconds)
    {
        CPH.TryGetArg<String>("scene", out string alertNesterScene);
        CPH.TryGetArg<String>("label", out string alertSource);
        CPH.SetGlobalVar("timeInSeconds", countdownInSeconds, true);
        
        
        CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, true);
        if (countdownInSeconds > 0)
        {
            int timeToAdd = CPH.GetGlobalVar<int>("timeToAdd", true);
            if (timeToAdd != 0)
            {
                countdownInSeconds += timeToAdd;
                CPH.LogDebug($"Adding {timeToAdd} seconds to Timer now");
                CPH.SetGlobalVar("timeToAdd", 0, true);
                CPH.SetGlobalVar("timeInSeconds", countdownInSeconds, true);
            }
            // Refetch every cycle to check for updates due to donos / commands            
            int hours = countdownInSeconds / 3600;
            int minutes = (countdownInSeconds - (hours * 3600)) / 60;
            int seconds = (countdownInSeconds - (hours * 3600)) - (minutes * 60);

            CPH.ObsSetGdiText(alertNesterScene, alertSource, $"Countdown: {hours:D2}:{minutes:D2}:{seconds:D2}");
            CPH.SetGlobalVar("timeToAdd", -1, true);
            CPH.Wait(1000);
            
            CPH.RunAction("PattoTimer", false);                    
        }
        else
        {
            CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, false);
        }
    }

    public bool PauseTimer()
    {
        CPH.PauseActionQueue("TimerQueue");
        return true;
    }

    public bool ResumeTimer()
    {
        CPH.ResumeActionQueue("TimerQueue");
        return true;
    }
}
