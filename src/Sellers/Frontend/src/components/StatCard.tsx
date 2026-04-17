type StatCardProps = {
  title: string;
  value: string;
};

export function StatCard({ title, value }: StatCardProps) {
  return (
    <div style={{ border: '1px solid #dbe3ef', borderRadius: 8, padding: 16, minWidth: 180 }}>
      <div style={{ color: '#475569', fontSize: 13 }}>{title}</div>
      <div style={{ marginTop: 8, fontSize: 24, fontWeight: 600 }}>{value}</div>
    </div>
  );
}
