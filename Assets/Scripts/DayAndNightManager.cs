using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class DayAndNightManager : MonoBehaviour
{
    public Text timeInGame;
    public float dayMultiplier = 20f;
    public Light2D light2D;
    public Gradient gradient;
    private void Update()
    {
        DateTime time = DateTime.Now;

        float realSecond = (time.Hour * 3600) + ( time.Minute * 60) + time.Second;
        realSecond = (realSecond * dayMultiplier) % 86400;

        int hours = Mathf.FloorToInt(realSecond / 3600);
        int minutes = Mathf.FloorToInt(realSecond % 3600) / 60;

        timeInGame.text = string.Format("{0:00}:{1:00}",hours,minutes);

        ChangeColorByTime(realSecond);
    }

    public void ChangeColorByTime(float time)
    {
        light2D.color = gradient.Evaluate(time/ 86400);
    }

}
