// Driver photo assets are hand-curated static files (AD-10), not a new
// external API. Path convention only — see frontend/public/fan-card-assets/drivers/README.md.
export function driverPhotoUrl(driverId: string): string {
  return `/fan-card-assets/drivers/${driverId}.jpg`
}
