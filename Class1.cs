using Streamer.bot.Common.Events;
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
        // Set sensible Defaults
        if(!CPH.TryGetArg<int>("secondsPerDollar", out int secondsPerDollar))
        {
            secondsPerDollar = 30;
        }
        if (!CPH.TryGetArg<int>("secondsPerHundredBits", out int secondsPerHundredBits))
        {
            secondsPerHundredBits = 30;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionPrime", out int secondsPerSubscriptionPrime))
        {
            secondsPerSubscriptionPrime = 150;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT1", out int secondsPerSubscriptionT1))
        {
            secondsPerSubscriptionT1 = 150;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT2", out int secondsPerSubscriptionT2))
        {
            secondsPerSubscriptionT2 = 250;
        }
        if (!CPH.TryGetArg<int>("secondsPerSubscriptionT3", out int secondsPerSubscriptionT3))
        {
            secondsPerSubscriptionT3 = 500;
        }

        int timeAdded = 0;
        int amountOfSubs = 1;
        
        // Find out the trigger and translate it to seconds
        EventType evt = CPH.GetEventType();
        CPH.LogDebug($"Triggered by {evt}");
        switch (evt)
        {
            case EventType.TwitchSub:
            case EventType.TwitchGiftBomb:
                if(CPH.TryGetArg<int>("gifts", out int gifts))
                {
                    amountOfSubs = gifts;
                }
                CPH.LogDebug("Triggered by evt");

                // Find out more about the sub/gift bomb
                CPH.TryGetArg<string>("tier", out string tier);
                switch(tier)
                {
                    case "prime":
                        timeAdded = secondsPerSubscriptionPrime * amountOfSubs;
                        break;
                    case "tier 1":
                        timeAdded = secondsPerSubscriptionT1 * amountOfSubs;
                        break;
                    case "tier 2":
                        timeAdded = secondsPerSubscriptionT2 * amountOfSubs;
                        break;
                    case "tier 3":
                        timeAdded = secondsPerSubscriptionT3 * amountOfSubs;
                        break;
                }
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for {amountOfSubs} * {tier} subs!");
                break;
            case EventType.TwitchCheer:
                CPH.TryGetArg<int>("bits", out int bits);
                float secondsPerBit = (float)secondsPerHundredBits / 100;
                CPH.LogDebug($"calculated {secondsPerBit} secondsPerBit");
                timeAdded = (int)Math.Floor(new decimal((float)bits * secondsPerBit));
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for Cheer of {bits} bits");
                break;
            case EventType.StreamElementsTip:
                CPH.TryGetArg<int>("tipAmount", out int donationInDollar);
                timeAdded = (donationInDollar * secondsPerDollar);
                CPH.LogDebug($"Calculated additional {timeAdded} seconds for Tip of {donationInDollar}");
                break;
        }


        if (timeAdded > 0)
        {
            int oldTime = CPH.GetGlobalVar<int>("timeToAdd", true);
            CPH.LogDebug($"Calculated {timeAdded} additional seconds for {evt}");
            if(oldTime > 0)
            {
                CPH.LogDebug($"Adding remnant of unadded {oldTime} on top of {timeAdded}");
                timeAdded += oldTime;
            }
            CPH.LogDebug($"Setting timeToAdd to {timeAdded} now");
            CPH.SetGlobalVar("timeToAdd", timeAdded, true);
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
