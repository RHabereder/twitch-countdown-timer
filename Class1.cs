using Streamer.bot.Plugin.Interface;
using System;

public class CPHInline: CPHInlineBase
{
    /// <summary>
    ///     Streamerbot Action that adds Time to a timer
    ///     If secondsPerDollar is not defined, it will default to 30 seconds per Dollar
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    ///     Streamerbot action that starts the timer
    /// </summary>
    /// <returns></returns>
    public bool StartTimer()
    {
        int timeInSeconds = CPH.GetGlobalVar<int>("timeInSeconds", true);
        if(timeInSeconds > 0)
        {
            ShowCountdown(timeInSeconds);
        } 
            
        return true;
    }

    /// <summary>
    ///     Private Utility-Method that handles the state and actual Time Arithmetic, as well as decreasing the time
    ///     It is a recursive method that will keep calling the Streamerbot Action "CountdownTimer", until the timer reaches 0
    ///     It relies on the args "scene" and "label" to find the correct OBS Label to set
    ///     If you want a prefix to your countdown, you can use the "countdownPrefix" Argument, which is "Countdown :" by default
    /// </summary>
    /// <param name="countdownInSeconds"></param>
    private void ShowCountdown(int countdownInSeconds)
    {
        CPH.TryGetArg<String>("scene", out string alertNesterScene);
        CPH.TryGetArg<String>("label", out string alertSource);
        CPH.TryGetArg<String>("countdownPrefix", out string countdownPrefix);
        CPH.SetGlobalVar("timeInSeconds", countdownInSeconds, true);

        CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, true);
        if (countdownInSeconds > 0)
        {
            UpdateTimerLabel(countdownInSeconds, alertNesterScene, alertSource, countdownPrefix);
            CPH.SetGlobalVar("timeToAdd", -1, true);
            CPH.Wait(1000);

            CPH.RunAction("CountdownTimer", false);
        }
        else
        {
            CPH.ObsSetSourceVisibility(alertNesterScene, alertSource, false);
        }
    }

    private void UpdateTimerLabel(int countdownInSeconds, string alertNesterScene, string alertSource, string countdownPrefix)
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

        CPH.ObsSetGdiText(alertNesterScene, alertSource, $"{countdownPrefix} {hours:D2}:{minutes:D2}:{seconds:D2}");
    }

    /// <summary>
    ///     Streamerbot Action to pause the TimerQueue
    /// </summary>
    /// <returns></returns>
    public bool PauseTimer()
    {
        CPH.PauseActionQueue("CountdownTimerQueue");
        return true;
    }

    /// <summary>
    ///     Streamerbot Action to resume the TimerQueue
    /// </summary>
    /// <returns></returns>
    public bool ResumeTimer()
    {
        CPH.ResumeActionQueue("CountdownTimerQueue");
        return true;
    }
}
