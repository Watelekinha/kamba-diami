/** Província de Angola, identificada pelo slug da API angolaprovinciasapi.ao (ex.: "luanda"). */
export interface Province {
  slug: string;
  name: string;
}

/** Município de Angola, identificado pelo slug da API angolaprovinciasapi.ao (ex.: "talatona"). */
export interface Municipality {
  slug: string;
  name: string;
  provinceSlug: string;
}

export interface ServiceCategory {
  id: number;
  name: string;
}
