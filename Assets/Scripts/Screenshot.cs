using System;
using System.IO;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    private string folder;

    [SerializeField]
    private string inputButton = "Screenshot";

    private bool inputButtonIsMapped;

    [SerializeField]
    private KeyCode key = KeyCode.F12;

    [SerializeField]
    [Range(1, 4)]
    private int superSize = 1;

    public KeyCode Key { get { return this.key; } set { this.key = value; } }

    public int SuperSize { get { return this.superSize; } set { this.superSize = Mathf.Clamp(value, 1, 4); } }

    private static string GetFilename()
    {
        return string.Format("{0}.png", DateTime.Now.ToBinary());
    }

    public void Capture()
    {
        string filepath = Path.Combine(this.folder, Screenshot.GetFilename());
        ScreenCapture.CaptureScreenshot(filepath, this.SuperSize);
    }

    private void Awake()
    {
        this.folder = Application.dataPath;
        this.folder = this.folder.Substring(0, this.folder.Length - 7);
        this.folder = Path.Combine(this.folder, "Assets/Screenshots");
        if (!Directory.Exists(this.folder))
        {
            Directory.CreateDirectory(this.folder);
        }

        this.inputButtonIsMapped = this.IsInputButtonMapped();
    }

    private bool IsInputButtonMapped()
    {
        if (string.IsNullOrEmpty(this.inputButton))
        {
            return false;
        }

        try
        {
            Input.GetButton(this.inputButton);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(this.Key) || (this.inputButtonIsMapped && Input.GetButton(this.inputButton)))
        {
            this.Capture();
        }
    }
}
