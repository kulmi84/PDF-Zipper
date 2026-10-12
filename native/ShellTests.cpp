#include <windows.h>
#include <shobjidl.h>
#include <shlobj.h>
#include <vector>
#include <string>
#include <cstdio>
template<class T> class ComPtr {
    T* value = nullptr;
public:
    ~ComPtr() { if (value) value->Release(); }
    T* Get() const { return value; }
    T* operator->() const { return value; }
    T** operator&() { return &value; }
    explicit operator bool() const { return value != nullptr; }
};
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
        Check(SUCCEEDED(command->GetTitle(nullptr, &title)) && title && std::wstring(title) == L"PDF-Zipper", "German title");
        CoTaskMemFree(title);
        EXPCMDFLAGS flags = ECF_DEFAULT;
        Check(SUCCEEDED(command->GetFlags(&flags)) && flags == ECF_HASSUBCOMMANDS, "Submenu flag");
        {
            ComPtr<IEnumExplorerCommand> children;
            Check(SUCCEEDED(command->EnumSubCommands(&children)), "Submenu enumerator");
            if (!children) return 8;
            bool german = PRIMARYLANGID(GetUserDefaultUILanguage()) == LANG_GERMAN;
            const wchar_t* expected[] = { german ? L"PDFs verzahnen" : L"Interleave PDFs", german ? L"PDFs zusammenführen" : L"Merge PDFs", german ? L"Als PDF zusammenführen" : L"Merge as PDF" };
            GUID ids[3]{};
            for (int i = 0; i < 3; i++) {
                ComPtr<IExplorerCommand> child;
                ULONG fetched = 0;
                Check(children->Next(1, &child, &fetched) == S_OK && fetched == 1, "Child enumerated");
                if (!child) return 9;
                PWSTR name = nullptr;
                Check(SUCCEEDED(child->GetTitle(nullptr, &name)) && name && std::wstring(name) == expected[i], "Child title");
                CoTaskMemFree(name);
                child->GetCanonicalName(&ids[i]);
                child->GetFlags(&flags);
                Check(flags == ECF_DEFAULT, "Child is actionable");
            }
            Check(ids[0] != ids[1] && ids[0] != CommandId && ids[1] != CommandId, "Distinct command identities");
            ComPtr<IExplorerCommand> end;
            Check(children->Next(1, &end, nullptr) == S_FALSE, "Exactly three children");
            children->Reset(); children->Skip(1);
            ComPtr<IEnumExplorerCommand> copy;
            Check(SUCCEEDED(children->Clone(&copy)), "Enumerator clone");
            Check(copy && copy->Next(1, &end, nullptr) == S_OK, "Clone retains position");
        }
        PWSTR icon = nullptr;
        Check(SUCCEEDED(command->GetIcon(nullptr, &icon)) && icon && GetFileAttributesW(icon) != INVALID_FILE_ATTRIBUTES, "Menu icon exists");
        CoTaskMemFree(icon);
        EXPCMDSTATE state = ECS_ENABLED;
        Check(SUCCEEDED(command->GetState(nullptr, FALSE, &state)) && state == ECS_HIDDEN, "No selection hidden");
        Check(command->GetState(nullptr, FALSE, nullptr) == E_POINTER, "Null output rejected");
        wchar_t temp[MAX_PATH];
        if (!GetTempPathW(MAX_PATH, temp)) return 6;
        std::wstring base(temp);
        base += L"pdf-zipper-native-" + std::to_wstring(GetCurrentProcessId());
        std::vector<std::wstring> names{base + L"-Vorderseiten.pdf", base + L"-Rückseiten.pdf", base + L".txt", base + L"-third.pdf", base + L".docx", base + L".png"};
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
        Check(selectionState({ids[0], ids[1], ids[2]}) == ECS_HIDDEN, "Mixed three files hidden");
        Check(selectionState({ids[0], ids[1], ids[3]}) == ECS_ENABLED, "Three PDFs enabled");
        {
            ComPtr<IShellItemArray> selected;
            PCIDLIST_ABSOLUTE three[] = {ids[0], ids[1], ids[3]};
            Check(SUCCEEDED(SHCreateShellItemArrayFromIDLists(3, three, &selected)), "Three PDF selection");
            ComPtr<IEnumExplorerCommand> children;
            command->EnumSubCommands(&children);
            if (!children) return 10;
            for (int i = 0; i < 3; i++) {
                ComPtr<IExplorerCommand> child;
                if (children->Next(1, &child, nullptr) != S_OK) return 11;
                child->GetState(selected.Get(), FALSE, &state);
                Check(state == static_cast<EXPCMDSTATE>(i == 1 ? ECS_ENABLED : ECS_HIDDEN), "Three PDFs only concatenate");
                if (i == 0) Check(child->Invoke(selected.Get(), nullptr) == E_INVALIDARG, "Reject interleaving three PDFs");
            }
        }
        {
            ComPtr<IShellItemArray> mixed;
            PCIDLIST_ABSOLUTE selected[] = {ids[0], ids[4], ids[5]};
            Check(SUCCEEDED(SHCreateShellItemArrayFromIDLists(3, selected, &mixed)), "PDF Word image selection");
            Check(selectionState({ids[4], ids[5]}) == ECS_ENABLED, "Word/image parent shown");
            ComPtr<IEnumExplorerCommand> children;
            command->EnumSubCommands(&children);
            for (int i=0; i<3; i++) {
                ComPtr<IExplorerCommand> child;
                children->Next(1, &child, nullptr);
                child->GetState(mixed.Get(), FALSE, &state);
                if (i<2) Check(state==ECS_HIDDEN, "PDF-only actions hidden for Word/images");
                else {
                    Check(state==ECS_ENABLED || state==ECS_DISABLED, "Document action shown");
                    // Override HKCR only in this test process to simulate a PC without Word.
                    HKEY empty = nullptr;
                    std::wstring key = L"Software\\PDF-Zipper-Word-Test-" + std::to_wstring(GetCurrentProcessId());
                    if (RegCreateKeyExW(HKEY_CURRENT_USER,key.c_str(),0,nullptr,REG_OPTION_VOLATILE,KEY_ALL_ACCESS,nullptr,&empty,nullptr)==ERROR_SUCCESS) {
                        Check(RegOverridePredefKey(HKEY_CLASSES_ROOT,empty)==ERROR_SUCCESS,"Simulate no Word");
                        child->GetState(mixed.Get(),FALSE,&state);
                        Check(state==ECS_DISABLED,"Word missing: merge disabled");
                        RegOverridePredefKey(HKEY_CLASSES_ROOT,nullptr);
                        RegCloseKey(empty); RegDeleteKeyW(HKEY_CURRENT_USER,key.c_str());
                    } else Check(false,"Create isolated Word test registry");
                    ComPtr<IShellItemArray> images;
                    PCIDLIST_ABSOLUTE imageSelection[] = {ids[0],ids[5]};
                    SHCreateShellItemArrayFromIDLists(2,imageSelection,&images);
                    child->GetState(images.Get(),FALSE,&state);
                    Check(state==ECS_ENABLED,"PDF/image does not require Word");
                }
            }
        }
        for (auto id : ids) CoTaskMemFree(id);
        for (const auto& name : names) DeleteFileW(name.c_str());
    }
    Check(canUnload() == S_OK, "Released COM objects allow unload");
    FreeLibrary(library); CoUninitialize();
    if (!failures) std::puts("PASS: native COM and selection checks");
    return failures ? 1 : 0;
}


