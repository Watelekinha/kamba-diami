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

/**
 * Bairro de um município. Os bairros já criados na plataforma têm id; as comunas da API que ainda
 * não foram usadas chegam como sugestão (id nulo) e são criadas quando alguém as escolhe.
 */
export interface Neighborhood {
  id: number | null;
  name: string;
  municipalitySlug: string;
  isSuggestion: boolean;
}

export interface ServiceCategory {
  id: number;
  name: string;
}
