import { useState } from 'react'
import { useNewsFeed } from '../../shared/api/news'
import type { NewsItem } from '../../shared/api/news'
import { formatSessionTime } from '../../shared/utils/dateUtils'

function NewsFeedSkeleton() {
  return (
    <div aria-hidden="true" className="flex flex-col gap-3">
      {[0, 1, 2, 3].map((key) => (
        <div
          key={key}
          className="h-20 animate-pulse rounded-lg border border-border-soft bg-bg-card motion-reduce:animate-none"
        />
      ))}
    </div>
  )
}

function NewsCard({ item }: { item: NewsItem }) {
  const [imageFailed, setImageFailed] = useState(false)
  const showImage = !!item.imageUrl && !imageFailed

  return (
    <a
      href={item.link}
      target="_blank"
      rel="noopener noreferrer"
      data-testid="news-card"
      className="flex items-center gap-3.5 rounded-lg border border-border-soft bg-bg-card px-[22px] py-[16px] no-underline hover:border-accent-editorial"
    >
      {showImage && (
        <img
          src={item.imageUrl}
          alt=""
          onError={() => setImageFailed(true)}
          className="h-[56px] w-[56px] shrink-0 rounded-md object-cover"
        />
      )}
      <div className="min-w-0">
        <h2 className="mb-1 text-[15px] font-bold tracking-[-0.01em] text-text-primary">{item.title}</h2>
        {item.snippet && (
          <p className="mb-1 truncate text-[12.5px] text-text-secondary">{item.snippet}</p>
        )}
        <p className="text-[12px] text-text-tertiary">
          {item.source} · {formatSessionTime(item.publishedAt)}
        </p>
      </div>
    </a>
  )
}

export function NewsFeedPage() {
  const { data, isPending, isError } = useNewsFeed()

  return (
    <div className="mx-auto max-w-[1100px] px-7 py-8 pb-16">
      <h1 className="mb-1 text-[26px] font-bold tracking-[-0.01em] text-text-primary">News Feed</h1>
      <p className="mb-7 text-[13px] text-text-secondary">
        Latest F1 headlines from Formula1.com, Autosport, and RaceFans.
      </p>

      {isPending && <NewsFeedSkeleton />}
      {isError && (
        <p role="alert" className="text-[13px] text-text-secondary">
          Couldn't reach the server — try refreshing.
        </p>
      )}
      {data && data.length === 0 && (
        <p className="text-[13px] text-text-secondary">No news available right now.</p>
      )}
      {data && data.length > 0 && (
        <ul className="flex flex-col gap-3">
          {data.map((item, index) => (
            <li key={`${item.link}-${index}`}>
              <NewsCard item={item} />
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
