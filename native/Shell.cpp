#include <windows.h>
#include <shobjidl.h>
#include <shlwapi.h>
#include <wrl.h>
#include <string>
#include <vector>
#include <atomic>
using Microsoft::WRL::ComPtr;
static HMODULE module;
static std::atomic<long> objects{0};
static const CLSID CommandId = {0xab7df971,0x896a,0x4d3b,{0xb9,0x8d,0xc6,0x94,0xb0,0x1e,0x7e,0xad}};

static HRESULT Paths(IShellItemArray* items, std::vector<std::wstring>& paths) {
    DWORD count = 0;
    if (!items || FAILED(items->GetCount(&count)) || count != 2) return E_INVALIDARG;
    for (DWORD i = 0; i < count; i++) {
        ComPtr<IShellItem> item; PWSTR raw = nullptr;
        HRESULT hr = items->GetItemAt(i, &item);
        if (FAILED(hr)) return hr;
        hr = item->GetDisplayName(SIGDN_FILESYSPATH, &raw);
        if (FAILED(hr)) return hr;
        std::wstring path(raw); CoTaskMemFree(raw);
        auto dot = path.find_last_of(L'.');
        if (dot == std::wstring::npos || _wcsicmp(path.c_str() + dot, L".pdf") != 0) return E_INVALIDARG;
        paths.push_back(std::move(path));
    }
    return S_OK;
}

class Command final : public IExplorerCommand {
    std::atomic<ULONG> refs{1};
public:
    Command() { ++objects; }
    ~Command() { --objects; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** out) override {
        if (!out) return E_POINTER; *out = nullptr;
        if (iid == IID_IUnknown || iid == IID_IExplorerCommand) { *out = static_cast<IExplorerCommand*>(this); AddRef(); return S_OK; }
        return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs; }
    ULONG STDMETHODCALLTYPE Release() override { auto n = --refs; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE GetTitle(IShellItemArray*, PWSTR* text) override { return SHStrDupW(L"PDFs verzahnen", text); }
    HRESULT STDMETHODCALLTYPE GetIcon(IShellItemArray*, PWSTR* text) override { if (!text) return E_POINTER; *text = nullptr; return E_NOTIMPL; }
    HRESULT STDMETHODCALLTYPE GetToolTip(IShellItemArray*, PWSTR* text) override { return SHStrDupW(L"Vorderseiten und umgekehrte Rückseiten abwechselnd zusammenführen", text); }
    HRESULT STDMETHODCALLTYPE GetCanonicalName(GUID* id) override { if (!id) return E_POINTER; *id = CommandId; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override {
        if (!state) return E_POINTER;
        std::vector<std::wstring> paths;
        *state = SUCCEEDED(Paths(items, paths)) ? ECS_ENABLED : ECS_HIDDEN;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE Invoke(IShellItemArray* items, IBindCtx*) override {
        std::vector<std::wstring> paths;
        HRESULT hr = Paths(items, paths); if (FAILED(hr)) return hr;
        std::vector<wchar_t> buffer(32768);
        DWORD size = GetModuleFileNameW(module, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (!size || size >= buffer.size()) return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        std::wstring exe(buffer.data(), size);
        exe = exe.substr(0, exe.find_last_of(L"\\/") + 1) + L"PDF-Zipper.exe";
        // Windows file names cannot contain quotes. Use CreateProcess directly, never a shell.
        std::wstring command = L"\"" + exe + L"\" \"" + paths[0] + L"\" \"" + paths[1] + L"\"";
        STARTUPINFOW startup{}; startup.cb = sizeof(startup); PROCESS_INFORMATION process{};
        if (!CreateProcessW(exe.c_str(), command.data(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &startup, &process))
            return HRESULT_FROM_WIN32(GetLastError());
        CloseHandle(process.hThread); CloseHandle(process.hProcess); return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetFlags(EXPCMDFLAGS* flags) override { if (!flags) return E_POINTER; *flags = ECF_DEFAULT; return S_OK; }
    HRESULT STDMETHODCALLTYPE EnumSubCommands(IEnumExplorerCommand** out) override { if (!out) return E_POINTER; *out = nullptr; return E_NOTIMPL; }
};

class Factory final : public IClassFactory {
    std::atomic<ULONG> refs{1};
public:
    Factory() { ++objects; } ~Factory() { --objects; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** out) override {
        if (!out) return E_POINTER; *out = nullptr;
        if (iid == IID_IUnknown || iid == IID_IClassFactory) { *out = static_cast<IClassFactory*>(this); AddRef(); return S_OK; } return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs; }
    ULONG STDMETHODCALLTYPE Release() override { auto n = --refs; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* outer, REFIID iid, void** out) override {
        if (!out) return E_POINTER; *out = nullptr; if (outer) return CLASS_E_NOAGGREGATION;
        auto instance = new(std::nothrow) Command(); if (!instance) return E_OUTOFMEMORY;
        auto hr = instance->QueryInterface(iid, out); instance->Release(); return hr;
    }
    HRESULT STDMETHODCALLTYPE LockServer(BOOL lock) override { if (lock) ++objects; else --objects; return S_OK; }
};
extern "C" HRESULT __stdcall DllGetClassObject(REFCLSID clsid, REFIID iid, void** out) {
    if (!out) return E_POINTER; *out = nullptr;
    if (clsid != CommandId) return CLASS_E_CLASSNOTAVAILABLE;
    auto factory = new(std::nothrow) Factory(); if (!factory) return E_OUTOFMEMORY;
    auto hr = factory->QueryInterface(iid, out); factory->Release(); return hr;
}
extern "C" HRESULT __stdcall DllCanUnloadNow() { return objects == 0 ? S_OK : S_FALSE; }
BOOL WINAPI DllMain(HINSTANCE instance, DWORD reason, LPVOID) {
    if (reason == DLL_PROCESS_ATTACH) { module = instance; DisableThreadLibraryCalls(instance); } return TRUE;
}
