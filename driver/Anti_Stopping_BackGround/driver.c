#include<ntifs.h>
#include <stdio.h>
#include<ntddk.h>
#define TERMIN_PM 0x1001
#define TERMIN 0x0001
#define TERMIN_F 0x1401
#define TERMIN_N 5136
#define TERMIN_MS 0x0010
PVOID obHandle;
typedef struct _LDR_DATA_TABLE_ENTRY64
{
	LIST_ENTRY64    InLoadOrderLinks;
	LIST_ENTRY64    InMemoryOrderLinks;
	LIST_ENTRY64    InInitializationOrderLinks;
	PVOID            DllBase;
	PVOID            EntryPoint;
	ULONG            SizeOfImage;
	UNICODE_STRING    FullDllName;
	UNICODE_STRING     BaseDllName;
	ULONG            Flags;
	USHORT            LoadCount;
	USHORT            TlsIndex;
	PVOID            SectionPointer;
	ULONG            CheckSum;
	PVOID            LoadedImports;
	PVOID            EntryPointActivationContext;
	PVOID            PatchInformation;
	LIST_ENTRY64    ForwarderLinks;
	LIST_ENTRY64    ServiceTagLinks;
	LIST_ENTRY64    StaticLinks;
	PVOID            ContextInformation;
	ULONG64            OriginalBase;
	LARGE_INTEGER    LoadTime;
} LDR_DATA_TABLE_ENTRY64, * PLDR_DATA_TABLE_ENTRY64;
extern UCHAR* PsGetProcessImageFileName
(
	_In_ PEPROCESS Process
);
char* getname(
	_In_ ULONG ulProcessID
)
{
	NTSTATUS  Status;
	PEPROCESS  EProcess = NULL;
	Status = PsLookupProcessByProcessId((HANDLE)ulProcessID, &EProcess);

	//通过句柄获取EProcess
	if (!NT_SUCCESS(Status)) {
		return FALSE;
	}
	ObDereferenceObject(EProcess);
	return (char*)PsGetProcessImageFileName(EProcess);
}

NTSTATUS DriverUnload(
	_In_ PDRIVER_OBJECT pDriverObj
)
{
	UNREFERENCED_PARAMETER(pDriverObj);
	DbgPrint("Stopping Anti-Stopping-Background\n");
	ObUnRegisterCallbacks(obHandle);
	return STATUS_SUCCESS;
}
OB_PREOP_CALLBACK_STATUS recall(
	_In_ PVOID RegistrationContext,
	_In_ POB_PRE_OPERATION_INFORMATION pOperationInformation
)
{
	HANDLE pid = PsGetProcessId((PEPROCESS)pOperationInformation->Object);
	char name[20] = { 0 };
	UNREFERENCED_PARAMETER(RegistrationContext);
	strcpy(name, getname((ULONG)pid));
	if (!_stricmp(name, "taskkill.exe"))
	{
		if (pOperationInformation->Operation == OB_OPERATION_HANDLE_CREATE)
		{
			pOperationInformation->Parameters->CreateHandleInformation.DesiredAccess = TERMIN_F;
		}
		return OB_PREOP_SUCCESS;
	}



	//    tmd  .net应用程序在内核中叫做 xxxx.ex
	if (!_stricmp(name, "ClassIsland.ex"))
	{
		//创建
		if (pOperationInformation->Operation == OB_OPERATION_HANDLE_CREATE)
		{
			int opcode = pOperationInformation->Parameters->CreateHandleInformation.OriginalDesiredAccess;
			//char str[20];
			//sprintf(str, "%d", opcode);
			//DbgPrint("ANTI-SB: GET NEW CODE:");
			//DbgPrint(str);
			if ((opcode == TERMIN_PM) || (opcode == TERMIN) || (opcode == TERMIN_F) 
				|| (opcode == TERMIN_MS) || (opcode == 0x0010) ||(opcode == 0x0012) 
				||(opcode == 0x1041) || (opcode == 0x0113) || (opcode == 0x101411)
				|| (opcode == 1)||(opcode==0x0010)||(opcode==0x0002))
			{

				//如果需要在检测到有人从外部终止进程后触发蓝屏 则将下面的内容取消注释
				//KdPrint(("BSOD...\n"));
				//KeBugCheckEx(0x000000EF, 0, 0, 0, 0);

				pOperationInformation->Parameters->CreateHandleInformation.DesiredAccess = 0x0;

			}

			if (opcode == 0x1041)
			{
				pOperationInformation->Parameters->CreateHandleInformation.DesiredAccess = STANDARD_RIGHTS_ALL;
			}
			//DbgPrint("\n");
		}
	}
	return OB_PREOP_SUCCESS;
}
NTSTATUS setcallback()
{

	OB_CALLBACK_REGISTRATION obReg;
	OB_OPERATION_REGISTRATION opReg;
	memset(&obReg, 0, sizeof(obReg));
	obReg.Version = ObGetFilterVersion();
	obReg.OperationRegistrationCount = 1;
	obReg.RegistrationContext = NULL;
	RtlInitUnicodeString(&obReg.Altitude, L"321000");
	memset(&opReg, 0, sizeof(opReg));
	opReg.ObjectType = PsProcessType;
	opReg.Operations = OB_OPERATION_HANDLE_CREATE | OB_OPERATION_HANDLE_DUPLICATE;
	opReg.PreOperation = (POB_PRE_OPERATION_CALLBACK)&recall;
	obReg.OperationRegistration = &opReg;
	return ObRegisterCallbacks(&obReg, &obHandle);
}
NTSTATUS DriverEntry(
	_In_ PDRIVER_OBJECT pDriverObj,
	_In_ PUNICODE_STRING pRegistryString
)
{
	NTSTATUS status = STATUS_SUCCESS;
	PLDR_DATA_TABLE_ENTRY64 pldr;

	pDriverObj->DriverUnload = DriverUnload;
	pldr = (PLDR_DATA_TABLE_ENTRY64)pDriverObj->DriverSection;
	pldr->Flags |= 0x20;
	setcallback();
	DbgPrint("Started Anti-Stopping-Background\n");
	return STATUS_SUCCESS;
}