#include <windows.h>
#include <shobjidl.h>
#include <shlwapi.h>
#include <string>
#include <vector>
#include <atomic>
static HMODULE module;
static std::atomic<long> objects{0};
static const CLSID CommandId = {0xab7df971,0x896a,0x4d3b,{0xb9,0x8d,0xc6,0x94,0xb0,0x1e,0x7e,0xad}};

static HRESULT Paths(IShellItemArray* items, std::vector<std::wstring>& paths) {
    DWORD count = 0;
    if (!items || FAILED(items->GetCount(&count)) || count < 2) return E_INVALIDARG;
    for (DWORD i = 0; i < count; i++) {
        IShellItem* item = nullptr; PWSTR raw = nullptr;
        HRESULT hr = items->GetItemAt(i, &item);
        if (FAILED(hr)) return hr;
        hr = item->GetDisplayName(SIGDN_FILESYSPATH, &raw);
        item->Release();
        if (FAILED(hr)) return hr;
        std::wstring path(raw); CoTaskMemFree(raw);
        auto dot = path.find_last_of(L'.');
        if (dot == std::wstring::npos || _wcsicmp(path.c_str() + dot, L".pdf") != 0) return E_INVALIDARG;
        paths.push_back(std::move(path));
    }
    return S_OK;
}

static HRESULT CreateEnumerator(IEnumExplorerCommand** out);

class Command final : public IExplorerCommand {
    std::atomic<ULONG> refs{1};
    int action;
public:
    explicit Command(int mode = 0) : action(mode) { ++objects; }
    ~Command() { --objects; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** out) override {
        if (!out) return E_POINTER; *out = nullptr;
        if (iid == IID_IUnknown || iid == IID_IExplorerCommand) { *out = static_cast<IExplorerCommand*>(this); AddRef(); return S_OK; }
        return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs; }
    ULONG STDMETHODCALLTYPE Release() override { auto n = --refs; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE GetTitle(IShellItemArray*, PWSTR* text) override { return SHStrDupW(action == 0 ? L"PDF-Zipper" : action == 1 ? L"PDFs verzahnen" : L"PDFs zusammenführen", text); }
    HRESULT STDMETHODCALLTYPE GetIcon(IShellItemArray*, PWSTR* text) override {
        if (!text) return E_POINTER; *text = nullptr;
        std::vector<wchar_t> buffer(32768);
        DWORD size = GetModuleFileNameW(module, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (!size || size >= buffer.size()) return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        DWORD light = 1, bytes = sizeof(light);
        RegGetValueW(HKEY_CURRENT_USER, L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", L"AppsUseLightTheme", RRF_RT_REG_DWORD, nullptr, &light, &bytes);
        std::wstring path(buffer.data(), size);
        path = path.substr(0, path.find_last_of(L"\\/") + 1) + (light ? L"Assets\\menu-light.ico" : L"Assets\\menu-dark.ico");
        return SHStrDupW(path.c_str(), text);
    }
    HRESULT STDMETHODCALLTYPE GetToolTip(IShellItemArray*, PWSTR* text) override { return SHStrDupW(action == 0 ? L"PDF-Werkzeuge" : action == 1 ? L"Vorderseiten und umgekehrte Rückseiten abwechselnd zusammenführen" : L"PDFs in gewählter Reihenfolge zusammenführen", text); }
    HRESULT STDMETHODCALLTYPE GetCanonicalName(GUID* id) override { if (!id) return E_POINTER; *id = CommandId; id->Data1 += action; return S_OK; }
    HRESULT STDMETHODCALLTYPE GetState(IShellItemArray* items, BOOL, EXPCMDSTATE* state) override {
        if (!state) return E_POINTER;
        std::vector<std::wstring> paths;
        *state = SUCCEEDED(Paths(items, paths)) && (action != 1 || paths.size() == 2) ? ECS_ENABLED : ECS_HIDDEN;
        return S_OK;
    }
    HRESULT STDMETHODCALLTYPE Invoke(IShellItemArray* items, IBindCtx*) override {
        if (action == 0) return E_NOTIMPL;
        std::vector<std::wstring> paths;
        HRESULT hr = Paths(items, paths); if (FAILED(hr)) return hr;
        if (action == 1 && paths.size() != 2) return E_INVALIDARG;
        std::vector<wchar_t> buffer(32768);
        DWORD size = GetModuleFileNameW(module, buffer.data(), static_cast<DWORD>(buffer.size()));
        if (!size || size >= buffer.size()) return HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
        std::wstring exe(buffer.data(), size);
        exe = exe.substr(0, exe.find_last_of(L"\\/") + 1) + L"PDF-Zipper.exe";
        // Windows file names cannot contain quotes. Use CreateProcess directly, never a shell.
        std::wstring command = L"\"" + exe + L"\"" + (action == 2 ? L" --merge" : L"");
        for (const auto& path : paths) command += L" \"" + path + L"\"";
        STARTUPINFOW startup{}; startup.cb = sizeof(startup); PROCESS_INFORMATION process{};
        if (!CreateProcessW(exe.c_str(), command.data(), nullptr, nullptr, FALSE, 0, nullptr, nullptr, &startup, &process))
            return HRESULT_FROM_WIN32(GetLastError());
        CloseHandle(process.hThread); CloseHandle(process.hProcess); return S_OK;
    }
    HRESULT STDMETHODCALLTYPE GetFlags(EXPCMDFLAGS* flags) override { if (!flags) return E_POINTER; *flags = action == 0 ? ECF_HASSUBCOMMANDS : ECF_DEFAULT; return S_OK; }
    HRESULT STDMETHODCALLTYPE EnumSubCommands(IEnumExplorerCommand** out) override { if (!out) return E_POINTER; *out = nullptr; return action == 0 ? CreateEnumerator(out) : E_NOTIMPL; }
};

class Enumerator final : public IEnumExplorerCommand {
    std::atomic<ULONG> refs{1};
    ULONG position;
public:
    explicit Enumerator(ULONG start = 0) : position(start) { ++objects; }
    ~Enumerator() { --objects; }
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** out) override {
        if (!out) return E_POINTER; *out = nullptr;
        if (iid == IID_IUnknown || iid == IID_IEnumExplorerCommand) { *out = static_cast<IEnumExplorerCommand*>(this); AddRef(); return S_OK; }
        return E_NOINTERFACE;
    }
    ULONG STDMETHODCALLTYPE AddRef() override { return ++refs; }
    ULONG STDMETHODCALLTYPE Release() override { auto n = --refs; if (!n) delete this; return n; }
    HRESULT STDMETHODCALLTYPE Next(ULONG count, IExplorerCommand** commands, ULONG* fetched) override {
        if (!commands || (!fetched && count != 1)) return E_POINTER;
        if (fetched) *fetched = 0;
        ULONG done = 0;
        while (done < count && position < 2) {
            auto command = new(std::nothrow) Command(static_cast<int>(position + 1));
            if (!command) { if (fetched) *fetched = done; return E_OUTOFMEMORY; }
            commands[done++] = command; ++position;
        }
        if (fetched) *fetched = done;
        return done == count ? S_OK : S_FALSE;
    }
    HRESULT STDMETHODCALLTYPE Skip(ULONG count) override {
        ULONG available = 2 - position;
        position += count < available ? count : available;
        return count <= available ? S_OK : S_FALSE;
    }
    HRESULT STDMETHODCALLTYPE Reset() override { position = 0; return S_OK; }
    HRESULT STDMETHODCALLTYPE Clone(IEnumExplorerCommand** out) override {
        if (!out) return E_POINTER;
        *out = new(std::nothrow) Enumerator(position);
        return *out ? S_OK : E_OUTOFMEMORY;
    }
};
static HRESULT CreateEnumerator(IEnumExplorerCommand** out) {
    *out = new(std::nothrow) Enumerator();
    return *out ? S_OK : E_OUTOFMEMORY;
}
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


