# NavMesh — checklist để quái sang được map 2

## Triệu chứng

- Quái đứng ngáo, không đuổi qua được chỗ xa / map khác.
- `NavMeshAgent` báo đường partial/invalid hoặc không tiến được.

## Việc cần làm trong Editor

1. **Bake NavMesh trên toàn vùng chơi**  
   - Dùng `NavMesh Surface` (AI Navigation package) hoặc cửa sổ Navigation legacy.  
   - Đảm bảo **cả hai khu map** (và lối qua sông sau khi mở đêm) đều nằm trong volume bake.

2. **Kết nối hai vùng NavMesh**  
   - Nếu có khe / bậc / sông: thêm **NavMesh Link** (hoặc Off Mesh Link) tại chỗ qua được.  
   - Chiều cao agent bake phải khớp `NavMeshAgent` trên prefab Monster.

3. **Kiểm tra Agent**  
   - Radius / height / step height khớp bake settings.  
   - `Area Mask` không loại nhầm layer đường đi.

4. **Sau khi thêm prefab sông / cầu**  
   - Bake lại NavMesh.  
   - Runtime: `PhaseRiverCrossingGate` chỉ **bật mesh/collider** — mesh phải có NavMesh included hoặc có Link đúng chỗ.

## Code đã hỗ trợ

- `MonsterAI`: `CalculatePath` + fallback `SamplePosition`, stuck recovery, adaptive chase timeout.

---

Nếu vẫn không qua được: mở tab AI Navigation → Visualization và xem đường đi có **đứt đoạn** giữa hai vùng hay không.
