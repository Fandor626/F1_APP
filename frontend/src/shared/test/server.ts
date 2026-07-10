import { setupServer } from 'msw/node'
import { ergastHandlers } from '../mocks/handlers/ergastHandlers'
import { newsHandlers } from '../mocks/handlers/newsHandlers'

export const server = setupServer(...ergastHandlers, ...newsHandlers)
