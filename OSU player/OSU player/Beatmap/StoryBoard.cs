﻿using System;
using System.Collections.Generic;
using System.IO;
using OSU_player.Graphic;
namespace OSU_player.OSUFiles.StoryBoard
{
    public enum ElementType
    {
        Background,
        Video,
        Break,
        Colour,
        Sprite,
        Sample,
        Animation
    }
    public enum ElementLayer
    {
        Background,
        Fail,
        Pass,
        Foreground
    }
    public enum ElementOrigin
    {
        TopLeft,
        TopCentre,
        TopRight,
        CentreLeft,
        Centre,
        CentreRight,
        BottomLeft,
        BottomCentre,
        BottomRight
    }
    public enum ElementLoopType
    {
        LoopOnce,
        LoopForever
    }
    public enum EventType
    {
        //F - fade【隐藏(淡入淡出)】
        //M - move【移动】
        //S - scale【缩放】
        //V - vector scale (width and height separately)【矢量缩放(宽高分别变动)】
        //R - rotate【旋转】
        //C - colour【颜色】
        //L - loop【循环】
        //T - Event-triggered loop【事件触发循环】
        //P - Parameters【参数】
        //Play - 播放sample
        F,
        MX,
        MY,
        M,
        S,
        V,
        R,
        C,
        L,
        T,
        P,
        Play
    }
    public enum Triggertype
    {
        HitSoundClap,
        HitSoundFinish,
        HitSoundWhistle,
        Passing,
        Failing
    }
    public struct SBvar
    {
        public string name;
        public string replace;
    }
    public struct SBEvent
    {
        public EventType Type;
        public int easing;
        //0 - none【没有缓冲】
        //1 - start fast and slow down【开始快结束慢】
        //2 - start slow and speed up【开始慢结束快】
        public int startT;
        public int endT;
        public float startxF; //F,S,R(只用x),V 'F stands for float-option
        public float startyF;
        public float endyF;
        public float endxF;
        public int startx; //M,MX,MY（只用x/y)
        public int starty;
        public int endx;
        public int endy;
        //P只用startx H - 水平翻转(0) V - 垂直翻转(1) A - additive-blend colour (2)
        public int r1; //C
        public int g1;
        public int b1;
        public int r2;
        public int g2;
        public int b2;
        public int volume; //Play
    }
    public struct TriggerEvent
    {
        public int triggerstart;
        public int triggerend;
        public SBEvent[] events;
        public int count;
    }
    public class SBelement
    {
        public List<TActionNode> F = new List<TActionNode>();
        public List<TActionNode> X = new List<TActionNode>();
        public List<TActionNode> Y = new List<TActionNode>();
        public List<TActionNode> SX = new List<TActionNode>();
        public List<TActionNode> SY = new List<TActionNode>();
        public List<TActionNode> R = new List<TActionNode>();
        public List<TActionNode> C = new List<TActionNode>();
        public List<TActionNode> P = new List<TActionNode>();
        public ElementType Type;
        public ElementLayer Layers;
        public ElementOrigin Origin; //sample时无
        public string path;
        public int x; //sample时无时无
        public int y;
        //Animation only
        public int frameCount;
        public int framedelay;
        public int lasttime = Int32.MinValue;
        public int starttime = Int32.MaxValue;
        public ElementLoopType Looptype; //默认LoopForever【一直循环】
        //事件触发循环可以被游戏时间事件触发. 虽然叫做循环, 事件触发循环循环时只执行一次
        //触发器循环和普通循环一样是从零计数. 如果两个重叠, 第一个将会被停止且被一个从头开始的循环替代.
        //如果他们和任何存在的故事版事件重叠,他们将不会循环直到那些故事版事件不在生效
    }
    public class StoryBoard
    {
        public List<SBelement> elements = new List<SBelement>();
        //TODO:单独抽取trigger并作索引
        public List<SBvar> Variables = new List<SBvar>();
        //public Dictionary<Triggertype, TriggerEvent> trigger = new Dictionary<Triggertype, TriggerEvent>();
        //目录由beatmapfiles.location-->beatmap.location
        private string picknext(ref string str, bool change = true)
        {
            string ret = "";
            if (!str.Contains(","))
            {
                ret = str;
                if (change) { str = ""; }
            }
            else
            {
                ret = str.Substring(0, str.IndexOf(","));
                if (change) { str = str.Substring(str.IndexOf(",") + 1); }
            }
            return ret;
        }
        private int color(int r, int g, int b)
        {
            return (b << 0x10) | (g << 8) | r;
        }
        private string dealevents(int element, StreamReader reader)
        {
            string row = "";
            row = reader.ReadLine();
            while (!reader.EndOfStream)
            {
                if (row.Trim() == "") { row = reader.ReadLine(); continue; }
                if (row.StartsWith("//") || row.Length == 0) { row = reader.ReadLine(); continue; }
                if (row.StartsWith("[")) { return (row); }
                if (!row.StartsWith(" ")) { return (row); }
                //do variables change first
                foreach (SBvar tmpvar in Variables)
                {
                    if (row.Contains(tmpvar.name)) { row.Replace(tmpvar.name, tmpvar.replace); }
                }
                if (row.StartsWith(" L"))
                {
                    //④对于L的处理：直接复制_L,time difference,loopcount
                    row = reader.ReadLine();
                    picknext(ref row);
                    int easing = Convert.ToInt32(picknext(ref row));
                    int loopcount = Convert.ToInt32(picknext(ref row));
                    //tmpe.easing:time difference
                    //time difference : 循环开始的时间和此系列SB事件第一次生效的最初时间之间的时间差, 单位是毫秒
                    //tmpe.startT:loopcount
                    while (!reader.EndOfStream && row.StartsWith("  "))
                    {
                        for (int i = 0; i <= loopcount; i++)
                        {
                            dealevent((row.Substring(1)), element, i * easing);
                        }
                        row = reader.ReadLine();
                    }
                    return (row);
                }
                else if (row.StartsWith(" T"))
                {
                    row = reader.ReadLine();
                    while (!reader.EndOfStream && row.StartsWith("  "))
                    {
                        //For i As Integer = 0 To tmpe.startT
                        //    dealevent(raw(currentrow).Substring(1), element, i * tmpe.easing, currentrow)
                        //    currentrow -= 1
                        //Next
                        row = reader.ReadLine();
                    }
                    return (row);
                }
                else
                {
                    dealevent(row, element, 0);
                    row = reader.ReadLine();
                }
            }
            return ("");
        }
        private void addevents(int element, EventType type, int time, int timeE, ref string values, int easing)
        {
            float value1f;
            float value2f;
            float value3f;
            float value4f;
            int value1;
            int value2;
            int value3;
            int value4;
            int value5;
            int value6;
            switch (type)
            {
                case EventType.F:
                    //③_M,0,1000,,320,240,320,240-->_M,0,1000,,320,240 (开始结束值相同）
                    value1f = Convert.ToSingle(picknext(ref values)) * 255;
                    if (picknext(ref values, false) == "") { value2f = value1f; picknext(ref values); }
                    else { value2f = Convert.ToSingle(picknext(ref values)) * 255; }
                    elements[element].F.Add(new TActionNode(time, timeE, value1f, value2f, easing));
                    break;
                case EventType.MX:
                    value1 = Convert.ToInt32(picknext(ref values));
                    if (picknext(ref values, false) == "") { value2 = value1; picknext(ref values); }
                    else { value2 = Convert.ToInt32(picknext(ref values)); }
                    elements[element].X.Add(new TActionNode(time, timeE, value1, value2, easing));
                    break;
                case EventType.MY:
                    value1 = Convert.ToInt32(picknext(ref values));
                    if (picknext(ref values, false) == "") { value2 = value1; picknext(ref values); }
                    else { value2 = Convert.ToInt32(picknext(ref values)); }
                    elements[element].Y.Add(new TActionNode(time, timeE, value1, value2, easing));
                    break;
                case EventType.M:
                    value1 = Convert.ToInt32(picknext(ref values));
                    value2 = Convert.ToInt32(picknext(ref values));
                    if (picknext(ref values, false) == "") { value3 = value1; value4 = value2; picknext(ref values); picknext(ref values); }
                    else { value3 = Convert.ToInt32(picknext(ref values)); value4 = Convert.ToInt32(picknext(ref values)); }
                    elements[element].X.Add(new TActionNode(time, timeE, value1, value3, easing));
                    elements[element].Y.Add(new TActionNode(time, timeE, value2, value4, easing));
                    break;
                case EventType.S:
                    value1f = Convert.ToSingle(picknext(ref values));
                    if (picknext(ref values, false) == "") { value2f = value1f; picknext(ref values); }
                    else { value2f = Convert.ToSingle(picknext(ref values)); }
                    elements[element].SX.Add(new TActionNode(time, timeE, value1f, value2f, easing));
                    elements[element].SY.Add(new TActionNode(time, timeE, value1f, value2f, easing));
                    break;
                case EventType.V:
                    value1f = Convert.ToSingle(picknext(ref values)); value2f = Convert.ToSingle(picknext(ref values));
                    if (picknext(ref values, false) == "") { value3f = value1f; value4f = value2f; picknext(ref values); picknext(ref values); }
                    else { value3f = Convert.ToSingle(picknext(ref values)); value4f = Convert.ToSingle(picknext(ref values)); }
                    elements[element].SX.Add(new TActionNode(time, timeE, value1f, value3f, easing));
                    elements[element].SY.Add(new TActionNode(time, timeE, value2f, value4f, easing));
                    break;
                case EventType.R:
                    value1f = Convert.ToSingle(picknext(ref values));
                    if (picknext(ref values, false) == "") { value2f = value1f; picknext(ref values); }
                    else { value2f = Convert.ToSingle(picknext(ref values)); }
                    elements[element].R.Add(new TActionNode(time, timeE, value1f, value2f, easing));
                    break;
                case EventType.C:
                    value1 = Convert.ToInt32(picknext(ref values));
                    value2 = Convert.ToInt32(picknext(ref values));
                    value3 = Convert.ToInt32(picknext(ref values));
                    if (picknext(ref values, false) == "")
                    { value4 = value1; value5 = value2; value6 = value3; picknext(ref values); picknext(ref values); picknext(ref values); }
                    else
                    { value4 = Convert.ToInt32(picknext(ref values)); value5 = Convert.ToInt32(picknext(ref values)); value6 = Convert.ToInt32(picknext(ref values)); }
                    elements[element].C.Add(new TActionNode(time, timeE, color(value1, value2, value3), color(value4, value5, value6), easing));
                    break;
                default:
                    //throw (new FormatException("Failed to read .osb file"));
                    break;
            }
            if (elements[element].lasttime < timeE) { elements[element].lasttime = timeE; }
            if (elements[element].starttime > time) { elements[element].starttime = time; }
        }
        private void dealevent(string str, int element, int delta)
        {
            //SBEvent tmpe = new SBEvent();
            string tmp = "";
            EventType type = (EventType)Enum.Parse(typeof(EventType), picknext(ref str).Trim());
            int easing = Convert.ToInt32(picknext(ref str));
            int startT = Convert.ToInt32(picknext(ref str)) + delta;
            int endT;
            //②_M,0,1000,1000,320,240,320,240-->_M,0,1000,,320,240,320,240(开始结束时间相同）
            tmp = picknext(ref str);
            if (tmp == "") { endT = startT; } else { endT = Convert.ToInt32(tmp) + delta; }
            while (str != "")
            {
                if (type == EventType.P)
                {
                    switch (picknext(ref str))
                    {
                        case "H":
                            elements[element].P.Add(new TActionNode(startT, endT, 1, 1, 3));
                            break;
                        case "V":
                            elements[element].P.Add(new TActionNode(startT, endT, 2, 2, 3));
                            break;
                        case "A":
                            elements[element].P.Add(new TActionNode(startT, endT, 4, 4, 3));
                            break;
                    }
                    continue;
                }
                addevents(element, type, startT, endT, ref str, easing);
                //_event,easing,starttime,endtime,val1,val2,val3,...,valN
                delta = endT - startT;
                startT += delta;
                endT += delta;
            }
        }
        private void dealfile(StreamReader reader, ref int element)
        {
            string row;
            string[] tmp = null;
            SBelement tmpe;
            string Position = "";
            row = reader.ReadLine();
            while (!reader.EndOfStream)
            {
                if (row.Trim() == "") { row = reader.ReadLine(); continue; }
                if (row.StartsWith("//") || row.Length == 0) { row = reader.ReadLine(); continue; }
                if (row.StartsWith("["))
                {
                    Position = row.Substring(1, row.Length - 2);
                    row = reader.ReadLine(); continue;
                }
                switch (Position)
                {
                    case "Variables":
                        {
                            SBvar tmpvar = new SBvar();
                            tmpvar.name = row.Split(new char[] { '=' }, 2)[0];
                            tmpvar.replace = row.Split(new char[] { '=' }, 2)[1];
                            tmpvar.name.Substring(1, tmpvar.name.Length - 1);
                            Variables.Add(tmpvar);
                            row = reader.ReadLine();
                            break;
                        }
                    case "Events":
                        //do variables change first
                        foreach (SBvar tmpvar in Variables)
                        {
                            if (row.Contains(tmpvar.name)) { row.Replace(tmpvar.name, tmpvar.replace); }
                        }
                        if (row.StartsWith("Sample") || row.StartsWith("5,"))
                        {
                            //Sample,time,layer,"filepath",volume
                            tmp = row.Split(new char[] { ',' });
                            tmpe = new SBelement();
                            tmpe.Type = ElementType.Sample;
                            tmpe.Layers = (ElementLayer)(System.Enum.Parse(typeof(ElementLayer), tmp[2]));
                            tmpe.path = tmp[3].Substring(1, tmp[3].Length - 2);
                            elements.Add(tmpe);
                            element++;
                            SBEvent tmpev = new SBEvent();
                            tmpev.startT = Convert.ToInt32(tmp[1]);
                            tmpev.Type = EventType.Play;
                            if (tmp.Length < 5)
                            {
                                tmpev.volume = 100;
                            }
                            else
                            {
                                tmpev.volume = Convert.ToInt32(tmp[4]);
                            }
                            row = reader.ReadLine();
                            //  elements[element].events.Add(tmpev);
                        }
                        else if (row.StartsWith("Animation") || row.StartsWith("6,"))
                        {
                            //Animation,"layer","origin","filepath",x,y,frameCount,frameDelay,looptype
                            tmp = row.Split(new char[] { ',' });
                            tmpe = new SBelement();
                            tmpe.Type = ElementType.Animation;
                            tmpe.Layers = (ElementLayer)(System.Enum.Parse(typeof(ElementLayer), tmp[1]));
                            if (tmpe.Layers == ElementLayer.Fail) { row = reader.ReadLine(); continue; }
                            tmpe.Origin = (ElementOrigin)(System.Enum.Parse(typeof(ElementOrigin), tmp[2]));
                            tmpe.path = tmp[3].Substring(1, tmp[3].Length - 2);
                            tmpe.x = Convert.ToInt32(tmp[4]);
                            tmpe.y = Convert.ToInt32(tmp[5]);
                            tmpe.frameCount = Convert.ToInt32(tmp[6]);
                            tmpe.framedelay = (int)(Convert.ToDouble(tmp[7]));
                            tmpe.Looptype = (ElementLoopType)(System.Enum.Parse(typeof(ElementLoopType), tmp[8]));
                            elements.Add(tmpe);
                            element++;
                            row = dealevents(element, reader);
                            if (elements[element].F.Count != 0 && elements[element].F[0].SValue == 0)
                            { }
                            else
                            {
                                elements[element].F.Insert(0, new TActionNode(elements[element].starttime, elements[element].starttime, 255f, 255f, 0));
                            }
                            elements[element].F.Add(new TActionNode(elements[element].lasttime, elements[element].lasttime, 0f, 0f, 0));
                        }
                        else if (row.StartsWith("Sprite") || row.StartsWith("4,"))
                        {
                            //Sprite,"layer","origin","filepath",x,y
                            tmp = row.Split(new char[] { ',' });
                            tmpe = new SBelement();
                            tmpe.Type = ElementType.Sprite;
                            tmpe.Layers = (ElementLayer)(System.Enum.Parse(typeof(ElementLayer), tmp[1]));
                            if (tmpe.Layers == ElementLayer.Fail) { row = reader.ReadLine(); continue; }
                            tmpe.Origin = (ElementOrigin)(System.Enum.Parse(typeof(ElementOrigin), tmp[2]));
                            tmpe.path = tmp[3].Substring(1, tmp[3].Length - 2);
                            tmpe.x = Convert.ToInt32(tmp[4]);
                            tmpe.y = Convert.ToInt32(tmp[5]);
                            elements.Add(tmpe);
                            element++;
                            row = dealevents(element, reader);
                            if (elements[element].F.Count != 0 && elements[element].F[0].SValue == 0)
                            { }
                            else
                            {
                                elements[element].F.Insert(0, new TActionNode(elements[element].starttime, elements[element].starttime, 255f, 255f, 0));
                            }
                            elements[element].F.Add(new TActionNode(elements[element].lasttime, elements[element].lasttime, 0f, 0f, 0));
                        }
                        /*    else if (row.StartsWith("0,"))
                            {
                                tmp = row.Split(new char[] { ',' });
                                tmpe = new SBelement();
                                tmpe.Type = ElementType.Sprite;
                                tmpe.Layers = ElementLayer.Background;
                                tmpe.Origin = ElementOrigin.Centre;
                                tmpe.path = tmp[2].Substring(1, tmp[2].Length - 2);
                                elements.Add(tmpe);
                                element++;
                                row = dealevents(element, reader);
                            }*/
                        else { row = reader.ReadLine(); }
                        break;
                    default:
                        row = reader.ReadLine();
                        break;
                }
            }
        }
        public StoryBoard(string osu, string osb)
        {
            StreamReader reader;
            try
            {
                int currentelement = -1;
                using (reader = new StreamReader(osu)) { dealfile(reader, ref currentelement); }
                if (osb != null) { using (reader = new StreamReader(osb)) { dealfile(reader, ref currentelement); } }
            }
            catch (SystemException e)
            {
                throw (new FormatException("Failed to read .osb file", e));
            }

        }
    }
}
