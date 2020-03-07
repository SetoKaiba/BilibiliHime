using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestEditor
{
    [MenuItem("Ludo/Roll")]
    public static void Roll()
    {
        Home.Instance.Roll();
    }
    
    [MenuItem("Ludo/Select0")]
    public static void Select0()
    {
        Home.Instance.Select(0);
    }
    
    [MenuItem("Ludo/Select1")]
    public static void Select1()
    {
        Home.Instance.Select(1);
    }
    
    [MenuItem("Ludo/Select2")]
    public static void Select2()
    {
        Home.Instance.Select(2);
    }
    
    [MenuItem("Ludo/Select3")]
    public static void Select3()
    {
        Home.Instance.Select(3);
    }
    
    [MenuItem("Ludo/Select4")]
    public static void Select4()
    {
        Home.Instance.Select(4);
    }
    
}