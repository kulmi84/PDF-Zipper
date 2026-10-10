#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <wrl.h>
#include <vector>
#include <string>
#include <cstdio>
using Microsoft::WRL::ComPtr;
static const CLSID CommandId = {0xab7df971,0x896a,0x4d3b,{0xb9,0x8d,0xc6,0x94,0xb0,0x1e,0x7e,0xad}};
static int failures = 0;
static void Check(bool ok, const char* name) { if (!ok) { ++failures; std::printf("FAIL: %s\n", name); } }
int wmain() {
    if (FAILED(CoInitializeEx(nullptr, COINIT_APARTMENTTHREADED))) return 1;
    HMODULE library = LoadLibraryW(L"PdfZipperShell.dll");
    if (!library) return 2;
    auto getFactory = reinterpret_cast<HRESULT(__stdcall*)(REFCLSID, REFIID, void**)>(GetProcAddress(library, "DllGetClassObject"));
    auto canUnload = reinterpret_cast<HRESULT(__stdcall*)()>(GetProcAddress(library, "DllCanUnloadNow"));
    if (!getFactory || !canUnload) return 3;
    {
        ComPtr<IClassFactory> factory;
        Check(SUCCEEDED(getFactory(CommandId, IID_PPV_ARGS(&factory))), "Class factory");
        if (!factory) return 4;
        ComPtr<IExplorerCommand> command;
        Check(SUCCEEDED(factory->CreateInstance(nullptr, IID_PPV_ARGS(&command))), "Explorer command");
        if (!command) return 5;
        Check(canUnload() == S_FALSE, "Active COM object prevents unload");
        PWSTR title = nullptr;
        Check(SUCCEEDED(command->GetTitle(nullptr, &title)) && title && std::wstring(title) == L"PDFs verzahnen", "German title");
        CoTaskMemFree(title);
        EXPCMDSTATE state = ECS_ENABLED;
        Check(SUCCEEDED(command->GetState(nullptr, FALSE, &state)) && state == ECS_HIDDEN, "No selection hidden");
        Check(command->GetState(nullptr, FALSE, nullptr) == E_POINTER, "Null output rejected");
        wchar_t temp[MAX_PATH];
        if (!GetTempPathW(MAX_PATH, temp)) return 6;
        std::wstring base(temp);
        base += L"pdf-zipper-native-" + std::to_wstring(GetCurrentProcessId());
        std::vector<std::wstring> names{base + L"-Vorderseiten.pdf", base + L"-Rückseiten.pdf", base + L".txt"};
        std::vector<PIDLIST_ABSOLUTE> ids;
        for (const auto& name : names) {
            HANDLE file = CreateFileW(name.c_str(), GENERIC_WRITE, 0, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
            if (file == INVALID_HANDLE_VALUE) return 7;
            CloseHandle(file);
            PIDLIST_ABSOLUTE id = nullptr;
            Check(SUCCEEDED(SHParseDisplayName(name.c_str(), nullptr, &id, 0, nullptr)), "Parse selected file");
            ids.push_back(id);
        }
        auto selectionState = [&](std::vector<PCIDLIST_ABSOLUTE> selected) {
            ComPtr<IShellItemArray> items;
            HRESULT hr = SHCreateShellItemArrayFromIDLists(static_cast<UINT>(selected.size()), selected.data(), &items);
            Check(SUCCEEDED(hr), "Create shell selection");
            EXPCMDSTATE value = ECS_ENABLED;
            Check(SUCCEEDED(command->GetState(items.Get(), FALSE, &value)), "Read selection state");
            return value;
        };
        Check(selectionState({ids[0]}) == ECS_HIDDEN, "Single PDF hidden");
        Check(selectionState({ids[0], ids[1]}) == ECS_ENABLED, "Two PDFs enabled");
        Check(selectionState({ids[1], ids[0]}) == ECS_ENABLED, "Selection order independent");
        Check(selectionState({ids[0], ids[2]}) == ECS_HIDDEN, "Mixed file types hidden");
        Check(selectionState({ids[0], ids[1], ids[2]}) == ECS_HIDDEN, "Three files hidden");
        for (auto id : ids) CoTaskMemFree(id);
        for (const auto& name : names) DeleteFileW(name.c_str());
    }
    Check(canUnload() == S_OK, "Released COM objects allow unload");
    FreeLibrary(library); CoUninitialize();
    if (!failures) std::puts("PASS: native COM and selection checks");
    return failures ? 1 : 0;
}

