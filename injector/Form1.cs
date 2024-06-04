using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

class Injector
{
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = false)]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttribute, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    public static void DLLError(string message)
    {
        Console.WriteLine(message);
        Environment.Exit(-1);
    }

    public static void InjectDLL(int processId, string dllPath)
    {
        IntPtr hProcess = OpenProcess(0X1F0FFF, false, processId);
        if (hProcess == IntPtr.Zero) DLLError("Could not open the process.");

        IntPtr hKernel = GetModuleHandle("kernel32.dll");
        if (hKernel == IntPtr.Zero) DLLError("Could not get kernel32 handle.");

        IntPtr loadLibraryAddr = GetProcAddress(hKernel, "LoadLibraryA");
        if (loadLibraryAddr == IntPtr.Zero) DLLError("Could not get address for LoadLibraryA.");

        IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((dllPath.Length + 1) * Marshal.SizeOf(typeof(char))), 0x1000, 0x40);
        if (allocMemAddress == IntPtr.Zero) DLLError("Could not allocate memory in the process.");

        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(dllPath);
        UIntPtr bWritten;
        if (!WriteProcessMemory(hProcess, allocMemAddress, bytes, (uint)bytes.Length, out bWritten)) DLLError("Could not write in the process memory.");

        IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, IntPtr.Zero);
        if (hThread == IntPtr.Zero) DLLError("Could not create remote thread.");

        Console.WriteLine("Injected.");
    }

    public static int[] GetProcessIDsByName(string processName)
    {
        Process[] processes = Process.GetProcessesByName(processName);
        int[] processIDS = new int[processes.Length];

        for (int i = 0; i < processes.Length; i++)
        {
            processIDS[i] = processes[i].Id;
        }

        return processIDS;
    }
}

namespace injector
{
    public partial class Form1 : Form
    {

        private string selectedFileName;
        public Form1()
        {
            InitializeComponent();
            panel2.Visible = true;
            panel3.Visible = false;
            panel9.Visible = false;

            panel2.Parent = this;
            panel9.Parent = this;
            panel9.Location = panel2.Location;
            panel3.Parent = this;
            panel3.Location = panel2.Location;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "c:\\";
            // openFileDialog1.Filter = "Database files (*.dll)|*.dll";
            // openFileDialog1.FilterIndex = 0;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() != DialogResult.OK) { return; }

            selectedFileName = openFileDialog1.FileName;
            string cuttedFN = selectedFileName;
            if (cuttedFN.Length >= 30)
            {
                cuttedFN = cuttedFN.Substring(0, 30) + "...";
            }
            label22.Text = cuttedFN;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int[] processes = Injector.GetProcessIDsByName("00004B6C-Pixel Gun 3D");
            Injector.InjectDLL(processes[0], selectedFileName);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel2.Visible = true;
            panel9.Visible = false;
            panel3.Visible = false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            panel9.Visible = true;
            panel3.Visible = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            panel9.Visible = false;
            panel3.Visible = true;
        }
    }
}
