# Project: EMR BenhAnNgoaiKhoa CLI (Bệnh Án Ngoại Khoa Trực Tiếp Oracle EMR)

## Architecture
- **HIS Integration & Patient Retrieval**: Interacts with HIS backend (`api/HisTreatment/GetView`, `api/HisDhst/GetView`, `api/HisSereServTein/GetView`, `api/HisSereServExt/Get`) to query patient demographics, ICD-10, vital signs (DHST), pre-op lab tests, and PACS imaging conclusions.
- **Oracle EMR Direct Data Access**: Connects to Oracle STB `192.168.7.248:1521/orclstb` (`EMR_FINAL`/`EMR_FINAL`) via official `Integrate\EMR\MDB.dll`, `Integrate\EMR\EMR_MAIN.Library.dll`, and `Integrate\EMR\Oracle.ManagedDataAccess.dll`. Queries tables `THONGTINDIEUTRI` and `BENHANNGOAIKHOA`.
- **Medical Record Inheritance Engine**:
  - *Branch A (Patient History)*: Queries historical `BENHANNGOAIKHOA` records of the same `MABENHNHAN` to inherit personal/family medical history and allergy/habit flags.
  - *Branch B (Cohort Pathology Template)*: Queries Khoa 57 surgical records (`MAKHOA = '9'`) matching the pathology/ICD group (e.g. ACL tear, vertebral fracture, clavicle fracture) to inherit physical examination (`CoXuongKhop`), trauma course structure (`QuaTrinhBenhLy`), and surgical plan (`HuongDieuTri`).
- **Clinical Synthesis Engine (Offline Ngoại/CTCH)**: Local rule-based clinical synthesis adhering to `AGENTS.md` Rule 4 (explicit anatomical naming: L2, L3, L5; L4/5; 1/3 fractures; maneuvers: Lachman, Drawer, Lasegue) without external AI APIs.
- **Doctor Session Extraction**: Non-locking tail-seek extraction of active doctor session (`034727` - ThS.BS Nguyễn Hữu Sâm, `vmc` - BS Vũ Minh Cường, `hdc` - BS Hà Đức Cường) from `Logs\LogSession.txt` / `LogSystem.txt`, with CLI argument override (`--doctor`).
- **CLI & Execution Wrapper**: `HisEmrBenhAnCli.exe` with arguments (`<MaBN|MaQuanLy>`, `--dry-run`, `--save`, `--doctor`, `--facility`), batch script `HisEmrBenhAn.bat`, end-to-end latency < 3.0s.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | F1: Identifier Resolution | Accept 10-digit `MaBenhNhan` or 12-digit `MaQuanLy`/`TreatmentId` and cross-resolve across HIS and Oracle EMR | M1 | Survey Explorer 2 |
| 2 | F2: HIS Clinical Data Extraction | Retrieve patient demographics, admission reason, ICD-10, DHST (vital signs), pre-op labs (CTM, đông máu, sinh hóa), and PACS conclusions | M1 | Survey Explorer 2 |
| 3 | F3: Patient History Inheritance | Query prior `BENHANNGOAIKHOA` records for same patient to inherit personal/family history and risk factors | M1 | Survey Explorer 2 |
| 4 | F4: Cohort Template Inheritance | Query Khoa 57 (`MAKHOA = '9'`) historical records for matching pathology/ICD to inherit specialized orthopedic exam templates | M1 | Survey Explorer 2 |
| 5 | F5: Doctor Session Extraction | Non-locking extraction of active doctor ID/name from `LogSession.txt` / `LogSystem.txt` with CLI override | M2 | Survey Explorer 3 |
| 6 | F6: Offline Clinical Synthesis | Rule-based synthesis for `TomTatBenhAn`, `CoXuongKhop`, `TienLuong`, `HuongDieuTri` adhering to `AGENTS.md` Rule 4 | M2 | Survey Explorer 3 |
| 7 | F7: DTO 100% Assembly | Assemble complete `EMR_MAIN.BenhAnNgoaiKhoa` DTO (all 64 properties, zero blank required fields) | M2 | Survey Explorer 1 & 3 |
| 8 | F8: Oracle EMR Connection | Connect to `192.168.7.248:1521/orclstb` via `ERMDatabase.KetNoi` and `MDBConnection` with timeout & pooling | M3 | Survey Explorer 1 |
| 9 | F9: Direct DB Persistence & Verification | Execute `BenhAnNgoaiKhoaFunc.InsertOrUpdate`, verify via `BenhAnNgoaiKhoaFunc.Select`, handle exceptions | M3 | Survey Explorer 1 |
| 10 | F10: CLI Application & Safety Modes | Build `HisEmrBenhAnCli.exe` supporting `--dry-run` (default preview, no DB change) and `--save` (live DB write) | M4 | Survey Explorer 3 |
| 11 | F11: Batch Wrapper & Build Pipeline | Create `HisEmrBenhAn.bat`, configure `refs.rsp` & 64-bit `csc.exe` compilation, ensure < 3.0s latency | M4 | Survey Explorer 1 & 3 |
| 12 | F12: E2E Test Suite & Adversarial Hardening | Comprehensive 5-tier test suite verifying 100% feature coverage, edge cases, and real clinical scenarios | Final | Survey Explorers |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | HIS Patient Lookup & Inheritance Engine | F1, F2, F3, F4: Identifier resolution, clinical data fetch, patient & cohort inheritance queries | none | PLANNED |
| M2 | Doctor Session & Clinical Synthesis Engine | F5, F6, F7: Doctor session extraction, offline Ngoại/CTCH clinical synthesis, 100% DTO assembly | M1 | PLANNED |
| M3 | Oracle EMR Persistence & Verification | F8, F9: Database connection lifecycle, InsertOrUpdate, Select verification, error handling | M2 | PLANNED |
| M4 | CLI Application, Batch Wrapper & Toolchain | F10, F11: CLI interface, --dry-run / --save, console formatting, batch wrapper, build script | M3 | PLANNED |
| Final | E2E Test Suite Pass & Adversarial Hardening | F12: Pass 100% E2E test suite (Tiers 1-4) and Tier 5 adversarial hardening | M4 | PLANNED |

## Interface Contracts
### PatientDataExtractor ↔ BenhAnSynthesizer
- Input: `PatientClinicalData` (Demographics, InTime, ICD, DHST, Labs, PACS, PriorRecords, CohortTemplate)
- Output: `BenhAnNgoaiKhoa` DTO fully populated

### BenhAnSynthesizer ↔ EmrDatabaseWriter
- Input: `MDBConnection con`, `BenhAnNgoaiKhoa ba`
- Method: `bool SaveBenhAn(MDBConnection con, BenhAnNgoaiKhoa ba, out string error)`
- Method: `BenhAnNgoaiKhoa VerifyBenhAn(MDBConnection con, decimal maQuanLy)`

### EmrDatabaseWriter ↔ HisEmrBenhAnCli
- CLI Arguments: `<MaBN|MaQuanLy> [--dry-run] [--save] [--doctor <ID>] [--facility <HN|NB>]`
- Return Codes:
  * `0`: Success (Dry-run preview displayed or DB save & verification confirmed)
  * `1`: Patient not found in HIS or EMR
  * `2`: Database connection or SQL execution error
  * `3`: Clinical synthesis or DTO assembly validation failure
  * `4`: Invalid command-line arguments

## Code Layout
- Root directory: `e:\his-x64-28-11fix GDYK\his-x64`
- C# Source files:
  * `HisEmrBenhAnCli.cs` — Main CLI application, argument parser, console formatter, orchestrator
  * `EmrPatientExtractor.cs` — HIS query & Oracle patient/cohort inheritance engine
  * `EmrClinicalSynthesizer.cs` — Doctor session extraction & offline clinical synthesis
  * `EmrDatabaseWriter.cs` — Oracle EMR database connection, persistence, and verification
- Batch wrapper: `HisEmrBenhAn.bat`
- Build script: `build_his_emr_benhan.bat`
- Assembly references: `refs.rsp`, `Integrate\EMR\EMR_MAIN.Library.dll`, `Integrate\EMR\Oracle.ManagedDataAccess.dll`
- Tests: `Tests\E2E_EmrBenhAn\`
