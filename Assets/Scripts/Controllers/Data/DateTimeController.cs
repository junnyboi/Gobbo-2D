using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class DateTimeController : MonoBehaviour
{
    #region VARIABLE DECLARATION
    public static DateTimeController Instance;
    public float timeScale = 300f;   //change time speed: 200 => one hour takes 18 seconds
    public bool fastMode { get; protected set; } = false;
    public bool superFastMode { get; protected set; } = false;
    public bool superDuperFastMode { get; protected set; } = false;
    public bool supercalifragilisticexpialidociousMode { get; protected set; } = false;
    public bool displayMinutes = true;
    public bool displayHours = true;
    public bool displayDay = true;
    public bool displayMonth = true;
    public bool displayYear = true;
    public int startHour = 9, startDay = 9, startMonth = 7, startYear = 0;

    private string monthName;
    private bool leapYear = false;
    public float minute { get; protected set; }
    public float hour { get; protected set; }
    public float day { get; protected set; }
    public float second { get; protected set; }
    public float month { get; protected set; }
    public int year { get; protected set; }

    public event Action cbOnYearChanged;
    public event Action cbOnMonthChanged;
    public event Action cbOnDayChanged;
    public event Action cbOnHourChanged;

    #endregion

    #region MONOBEHAVIOUR
    void Awake()
    {
        Instance = this;
        year = startYear;
        month = startMonth;
        day = startDay;
        hour = startHour;
        year = startYear;

        SwitchMonthName();
    }
    void Update()
    {
       // Temporarily disabled to reduce development complexity
       // CalculateTime();

        if (fastMode)
            NewHour();
        if (superFastMode)
            NewDay();
        if (superDuperFastMode)
            NewMonth();
        if (supercalifragilisticexpialidociousMode)
            NewYear();
    }
    #endregion

    #region UI STRING DISPLAY
    public override string ToString()
    {
        string dateTimeString = "";

        if (displayYear)
            dateTimeString += "Year " + year;
        if (displayMonth)
            dateTimeString += ", " + monthName;
        if (displayDay)
            dateTimeString += " " + day;
        if (displayHours)
        {
            dateTimeString += ", ";
            if (hour < 12)
            {
                if (hour <= 9)
                    dateTimeString += "  0" + hour + DisplayMinutes() + " AM";
                else if (hour > 9)
                    dateTimeString += "  " + hour + DisplayMinutes() + " AM";
            }
            else if (hour >= 12)
            {
                dateTimeString += "  " + (hour - 12) + DisplayMinutes() + " PM";
            }
        }
        if (supercalifragilisticexpialidociousMode)
            dateTimeString += "\n" + "[Supercalifragilisticexpialidocious Speed]";
        else if (superDuperFastMode)
            dateTimeString += "\n" + "[Super Duper Fast Speed]";
        else if (superFastMode)
            dateTimeString += "\n" + "[Super Fast Speed]";
        else if (fastMode)
            dateTimeString += "\n" + "[Fast Speed]";
        else
            dateTimeString += "\n" + "[Normal Speed]";

        return dateTimeString;
    }

    string DisplayMinutes()
    {
        string dateTimeString = "";
        if (displayMinutes && displayHours)
        {
            if (minute <= 9)
                dateTimeString += ":0" + minute;

            else
                dateTimeString += ":" + minute;
        }

        return dateTimeString;
    }
    #endregion

    #region MAIN LOGIC
    //determining month names
    void SwitchMonthName()
    {
        switch (month)
        {
            case 1: monthName = "January"; break;
            case 2: monthName = "February"; break;
            case 3: monthName = "March"; break;
            case 4: monthName = "April"; break;
            case 5: monthName = "May"; break;
            case 6: monthName = "June"; break;
            case 7: monthName = "July"; break;
            case 8: monthName = "August"; break;
            case 9: monthName = "September"; break;
            case 10: monthName = "October"; break;
            case 11: monthName = "November"; break;
            case 12: monthName = "December"; break;
            default:
                break;
        }
        ToString();
    }
    //determining total days in a month and leap years
    void CalculateMonthLength()
    {
        switch (month)
        {
            case 2:
                if (day >= 29)
                {
                    // leap year
                    if (year % 4 == 0 && year % 100 != 0)
                    {
                        ToString();
                        SwitchMonthName();
                        leapYear = true;
                    }
                    // determine month calculations
                    if (leapYear == false)
                        NewMonth();
                    else if (day >= 30)
                        NewMonth();
                }
                break;
            case 4:
                if (day >= 31)
                    NewMonth();
                break;
            case 6:
                if (day >= 31)
                    NewMonth();
                break;
            case 9:
                if (day >= 31)
                    NewMonth();
                break;
            case 11:
                if (day >= 31)
                    NewMonth();
                break;
            default:
                if (day >= 32)
                    NewMonth();
                break;
        }
    }

    //time counter
    void CalculateTime()
    {
        second += Time.fixedDeltaTime * timeScale;
        if (second >= 60)
        {
            minute++;
            second = 0;
            ToString();
        }
        else if (minute >= 60)
        {
            NewHour();
            minute = 0;
            ToString();
        }
        else if (hour >= 24)
        {
            NewDay();
            hour = 0;
        }
        else if (day >= 28)
        {
            CalculateMonthLength();
        }
        else if (month >= 12)
        {
            month = 1;
            NewYear();
            SwitchMonthName();
        }
    }
    #endregion

    #region NEW YEAR/MONTH/DAY CALLBACKS
    void NewYear()
    {
        year++;
        cbOnYearChanged?.Invoke();
    }

    void NewMonth()
    {
        month++;
        day = 1;
        SwitchMonthName();
        cbOnMonthChanged?.Invoke();
    }

    void NewDay()
    {
        day++;
        cbOnDayChanged?.Invoke();
    }
    void NewHour()
    {
        if (hour >= 24)
        {
            hour = 0;
            NewDay();
        }

        hour++;
        cbOnHourChanged?.Invoke();
    }
    #endregion

    #region UTILITIES
    public void ToggleSpeedUp()
    {
        if (!fastMode)
            fastMode = true;
        else if (!superFastMode)
            superFastMode = true;
        else if (!superDuperFastMode)
            superDuperFastMode = true;
        else if (!supercalifragilisticexpialidociousMode)
            supercalifragilisticexpialidociousMode = true;
        else
        {
            fastMode = false;
            superFastMode = false;
            superDuperFastMode = false;
            supercalifragilisticexpialidociousMode = false;
        }
    }
    public void ToggleSpeedDown()
    {
        if (supercalifragilisticexpialidociousMode)
            supercalifragilisticexpialidociousMode = false;
        else if (superDuperFastMode)
            superDuperFastMode = false;
        else if (superFastMode)
            superFastMode = false;
        else if (fastMode)
            fastMode = false;
        else
        {
            fastMode = true;
            superFastMode = true;
            superDuperFastMode = true;
            supercalifragilisticexpialidociousMode = true;
        }
    }

    public void TogglePause()
    {
        Time.timeScale = Time.timeScale == 0 ? SpeedSlider.Instance.slider.value : 0;
    }

    public void ChangeSpeed(float multiplier)
    {
        Time.timeScale = Time.timeScale = multiplier;
    }

    #endregion
}
