# PowPad 🐾

**PowPad** is a sleek, "always-on-top" AI productivity assistant for Windows. Built with C# and WPF, it provides a seamless bridge between your desktop environment and state-of-the-art Large Language Models.

---

## 🚀 The Workflow
1. **Highlight** any text in any application (Word, Notepad, VS Code, Browser).
2. **Click** the floating "MEOW" bubble.
3. **Receive** the AI-processed text automatically pasted back into your document.

---

## ✨ Features

* **Floating UI**: A minimal, non-intrusive violet bubble that stays on top of all windows.
* **Dual-AI Failover Pipeline**:
    * **Primary**: Google Gemini 2.5 Flash.
    * **Secondary**: Groq (Llama 3.3 70B).
    * *If Gemini hits a rate limit or fails, PowPad automatically switches to Groq to ensure zero downtime.*
* **Intelligent Dragging**: The bubble becomes semi-transparent while being moved to stay out of your visual way.
* **Visual State Feedback**:
    * 🟣 **Purple**: Idle & Ready.
    * 🟢 **Green**: Gemini is processing.
    * 🟠 **Orange**: Failover active (Gemini failed, calling Groq).
    * 🔴 **Red**: Error (No text selected or API connection issues).
* **Zero-Click Integration**: Uses Win32 API and SendKeys to automate the `Ctrl+C` -> `AI Process` -> `Ctrl+V` cycle.

---

## 🛠️ Technical Stack

* **Language**: C# / XAML
* **Framework**: .NET / WPF
* **APIs**: Google Generative AI (Gemini), Groq Cloud API
* **Libraries**: Newtonsoft.Json, DotNetEnv

---

## ⚙️ Installation & Setup

### 1. Prerequisites
* Visual Studio 2022
* .NET 6.0 or higher
* Gemini API Key ([Get it here](https://aistudio.google.com/))
* Groq API Key ([Get it here](https://console.groq.com/))

### 2. Configuration
Create a `.env` file in the root directory of the project:
```text
GEMINI_API_KEY=your_gemini_api_key_here
GROQ_API_KEY=your_groq_api_key_here
```

## WORKING



https://github.com/user-attachments/assets/2404c6f3-1c09-4fe7-8679-0553fe5036fa

