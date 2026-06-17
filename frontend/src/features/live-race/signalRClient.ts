import * as signalR from '@microsoft/signalr'

const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL
if (!hubUrl) {
  throw new Error('VITE_SIGNALR_HUB_URL is not defined. Add it to your .env.local file.')
}

export const raceHubConnection = new signalR.HubConnectionBuilder()
  .withUrl(hubUrl)
  .withAutomaticReconnect({
    nextRetryDelayInMilliseconds: (retryContext) => {
      const delays = [0, 2000, 5000, 10000, 30000, 60000]
      return delays[retryContext.previousRetryCount] ?? 60000
    },
  })
  .build()
