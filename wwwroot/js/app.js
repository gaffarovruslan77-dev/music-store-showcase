let currentLocale = 'en-US';
let currentSeed = 58933423;
let currentAverageLikes = 0;
let currentPage = 1;
let currentView = 'table';
const pageSize = 10;
let isLoadingGallery = false;
let currentAudio = null;
let currentAudioIndex = null;
let expandedRows = new Set();

document.addEventListener('DOMContentLoaded', function() {
    setupEventListeners();
    loadSongs();
    setupInfiniteScroll();
});

function setupEventListeners() {
    document.getElementById('localeSelect').addEventListener('change', function(e) {
        currentLocale = e.target.value;
        currentPage = 1;
        expandedRows.clear();
        if (currentView === 'gallery') {
            document.getElementById('galleryContainer').innerHTML = '';
        }
        loadSongs();
    });
    
    document.getElementById('seedInput').addEventListener('input', function(e) {
        const value = parseInt(e.target.value);
        if (!isNaN(value)) {
            currentSeed = value;
            currentPage = 1;
            expandedRows.clear();
            if (currentView === 'gallery') {
                document.getElementById('galleryContainer').innerHTML = '';
            }
            loadSongs();
        }
    });
    
    document.getElementById('randomSeedBtn').addEventListener('click', function() {
        currentSeed = Math.floor(Math.random() * 1000000000);
        document.getElementById('seedInput').value = currentSeed;
        currentPage = 1;
        expandedRows.clear();
        if (currentView === 'gallery') {
            document.getElementById('galleryContainer').innerHTML = '';
        }
        loadSongs();
    });
    
    document.getElementById('likesSlider').addEventListener('input', function(e) {
        currentAverageLikes = parseFloat(e.target.value);
        document.getElementById('likesValue').textContent = currentAverageLikes.toFixed(1);
        currentPage = 1;
        if (currentView === 'gallery') {
            document.getElementById('galleryContainer').innerHTML = '';
        }
        loadSongs();
    });
    
    document.getElementById('tableViewBtn').addEventListener('click', function() {
        switchView('table');
    });
    
    document.getElementById('galleryViewBtn').addEventListener('click', function() {
        switchView('gallery');
    });
}

function setupInfiniteScroll() {
    const galleryView = document.getElementById('galleryView');
    
    galleryView.addEventListener('scroll', function() {
        if (currentView !== 'gallery' || isLoadingGallery) return;
        
        const scrollPosition = galleryView.scrollTop + galleryView.clientHeight;
        const scrollHeight = galleryView.scrollHeight;
        
        if (scrollPosition >= scrollHeight - 100) {
            currentPage++;
            loadSongs(true);
        }
    });
}

function switchView(view) {
    currentView = view;
    
    if (view === 'table') {
        document.getElementById('tableView').style.display = 'block';
        document.getElementById('galleryView').style.display = 'none';
        document.getElementById('tableViewBtn').classList.add('active');
        document.getElementById('tableViewBtn').classList.remove('btn-outline-primary');
        document.getElementById('tableViewBtn').classList.add('btn-primary');
        document.getElementById('galleryViewBtn').classList.remove('active');
        document.getElementById('galleryViewBtn').classList.add('btn-outline-primary');
        document.getElementById('galleryViewBtn').classList.remove('btn-primary');
        currentPage = 1;
        loadSongs();
    } else {
        document.getElementById('tableView').style.display = 'none';
        document.getElementById('galleryView').style.display = 'block';
        document.getElementById('tableViewBtn').classList.remove('active');
        document.getElementById('tableViewBtn').classList.add('btn-outline-primary');
        document.getElementById('tableViewBtn').classList.remove('btn-primary');
        document.getElementById('galleryViewBtn').classList.add('active');
        document.getElementById('galleryViewBtn').classList.remove('btn-outline-primary');
        document.getElementById('galleryViewBtn').classList.add('btn-primary');
        currentPage = 1;
        document.getElementById('galleryContainer').innerHTML = '';
        loadSongs();
    }
}

async function loadSongs(append = false) {
    const url = `/api/songs?locale=${currentLocale}&seed=${currentSeed}&averageLikes=${currentAverageLikes}&page=${currentPage}&pageSize=${pageSize}`;
    
    if (currentView === 'gallery') {
        isLoadingGallery = true;
    }
    
    try {
        const response = await fetch(url);
        const data = await response.json();
        
        if (currentView === 'table') {
            renderTable(data.songs);
            renderPagination();
        } else {
            renderGallery(data.songs, append);
        }
    } catch (error) {
        console.error('Error loading songs:', error);
    } finally {
        if (currentView === 'gallery') {
            isLoadingGallery = false;
        }
    }
}

function renderTable(songs) {
    const tbody = document.getElementById('tableBody');
    tbody.innerHTML = '';
    
    songs.forEach(song => {
        const row = document.createElement('tr');
        row.classList.add('song-row');
        row.dataset.index = song.index;
        row.style.cursor = 'pointer';
        
        if (expandedRows.has(song.index)) {
            row.classList.add('expanded');
        }
        
        row.innerHTML = `
            <td>${song.index}</td>
            <td>${escapeHtml(song.title)}</td>
            <td>${escapeHtml(song.artist)}</td>
            <td>${escapeHtml(song.album)}</td>
            <td>${escapeHtml(song.genre)}</td>
        `;
        
        row.addEventListener('click', () => toggleRowExpansion(row, song));
        tbody.appendChild(row);
        
        if (expandedRows.has(song.index)) {
            createDetailsRow(row, song);
        }
    });
}

function toggleRowExpansion(row, song) {
    const index = song.index;
    const nextRow = row.nextElementSibling;
    
    if (nextRow && nextRow.classList.contains('expanded-row')) {
        nextRow.remove();
        row.classList.remove('expanded');
        expandedRows.delete(index);
        
        if (currentAudioIndex === index && currentAudio) {
            currentAudio.pause();
            currentAudio = null;
            currentAudioIndex = null;
        }
    } else {
        row.classList.add('expanded');
        expandedRows.add(index);
        createDetailsRow(row, song);
    }
}

function createDetailsRow(row, song) {
    const expandedRow = document.createElement('tr');
    expandedRow.classList.add('expanded-row');
    expandedRow.innerHTML = `
        <td colspan="5">
            <div class="song-details">
                <div class="row">
                    <div class="col-md-3">
                        <div class="album-cover">
                            <img src="${song.coverUrl}" alt="${escapeHtml(song.title)}" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22400%22><rect fill=%22%23667eea%22 width=%22400%22 height=%22400%22/></svg>'">
                        </div>
                        <div class="text-center mt-3">
                            <span class="likes-badge">${song.likes} ❤</span>
                        </div>
                    </div>
                    <div class="col-md-9">
                        <h5>
                            <button class="btn-play-inline" id="play-btn-${song.index}" onclick="toggleAudio(${song.index}, '${song.audioUrl}')">
                                ▶
                            </button>
                            ${escapeHtml(song.title)}
                            <button class="btn btn-sm btn-outline-secondary ms-2" onclick="toggleVolume(${song.index})">
                                🔊
                            </button>
                        </h5>
                        <p class="song-meta">
                            from <strong>${escapeHtml(song.album)}</strong> by <strong>${escapeHtml(song.artist)}</strong>
                        </p>
                        <p class="song-review mt-3">${escapeHtml(song.review)}</p>
                        
                        <div class="audio-player-container" id="audio-container-${song.index}" style="display: none;">
                            <div class="audio-player">
                                <audio id="audio-${song.index}" controls style="width: 100%;">
                                    <source src="${song.audioUrl}" type="audio/wav">
                                    Your browser does not support the audio element.
                                </audio>
                            </div>
                        </div>
                        
                        <div class="lyrics-container mt-3">
                            <button class="btn btn-sm btn-outline-secondary lyrics-toggle-btn" id="lyrics-btn-${song.index}" onclick="toggleLyrics(${song.index})">
                                Show Lyrics
                            </button>
                            <div class="lyrics-content" id="lyrics-${song.index}" style="display: none;">
                                ${formatLyrics(song.lyrics, song.index)}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </td>
    `;
    
    row.after(expandedRow);
    
    setTimeout(() => {
        const audio = document.getElementById(`audio-${song.index}`);
        if (audio) {
            audio.addEventListener('timeupdate', () => {
                syncLyrics(song.index, audio.currentTime, audio.duration);
            });
            
            audio.addEventListener('play', () => {
                const playBtn = document.getElementById(`play-btn-${song.index}`);
                if (playBtn) playBtn.innerHTML = '⏸';
            });
            
            audio.addEventListener('pause', () => {
                const playBtn = document.getElementById(`play-btn-${song.index}`);
                if (playBtn) playBtn.innerHTML = '▶';
            });
        }
    }, 100);
}

function toggleAudio(index, audioUrl) {
    const audioContainer = document.getElementById(`audio-container-${index}`);
    const audio = document.getElementById(`audio-${index}`);
    const playBtn = document.getElementById(`play-btn-${index}`);
    
    if (currentAudio && currentAudioIndex !== index) {
        currentAudio.pause();
        currentAudio.currentTime = 0;
        const oldContainer = document.getElementById(`audio-container-${currentAudioIndex}`);
        if (oldContainer) oldContainer.style.display = 'none';
        const oldPlayBtn = document.getElementById(`play-btn-${currentAudioIndex}`);
        if (oldPlayBtn) oldPlayBtn.innerHTML = '▶';
    }
    
    if (audioContainer.style.display === 'none') {
        audioContainer.style.display = 'block';
        currentAudio = audio;
        currentAudioIndex = index;
        audio.play();
        playBtn.innerHTML = '⏸';
    } else {
        if (audio.paused) {
            audio.play();
            playBtn.innerHTML = '⏸';
        } else {
            audio.pause();
            playBtn.innerHTML = '▶';
        }
    }
}

function toggleVolume(index) {
    const audio = document.getElementById(`audio-${index}`);
    if (audio) {
        audio.muted = !audio.muted;
    }
}

function toggleLyrics(index) {
    const lyricsDiv = document.getElementById(`lyrics-${index}`);
    const lyricsBtn = document.getElementById(`lyrics-btn-${index}`);
    
    if (lyricsDiv.style.display === 'none') {
        lyricsDiv.style.display = 'block';
        lyricsBtn.textContent = 'Hide Lyrics';
    } else {
        lyricsDiv.style.display = 'none';
        lyricsBtn.textContent = 'Show Lyrics';
    }
}

function formatLyrics(lyrics, index) {
    if (!lyrics) return '';
    
    const lines = lyrics.split('\n');
    return lines.map((line, i) => 
        `<p class="lyrics-line" data-line="${i}" data-song="${index}">${escapeHtml(line) || '&nbsp;'}</p>`
    ).join('');
}

function syncLyrics(songIndex, currentTime, duration) {
    const lyricsDiv = document.getElementById(`lyrics-${songIndex}`);
    if (!lyricsDiv || lyricsDiv.style.display === 'none' || !duration) return;
    
    const lines = lyricsDiv.querySelectorAll('.lyrics-line');
    const totalLines = lines.length;
    
    const progress = currentTime / duration;
    const activeLineIndex = Math.floor(progress * totalLines);
    
    lines.forEach((line, i) => {
        if (i === activeLineIndex) {
            line.classList.add('active');
            if (i > 2) {
                line.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        } else {
            line.classList.remove('active');
        }
    });
}

function renderGallery(songs, append = false) {
    const container = document.getElementById('galleryContainer');
    
    if (!append) {
        container.innerHTML = '';
    }
    
    songs.forEach(song => {
        const card = document.createElement('div');
        card.classList.add('gallery-card');
        card.innerHTML = `
            <div class="card">
                <div class="card-cover">
                    <img src="${song.coverUrl}" alt="${escapeHtml(song.title)}" onerror="this.src='data:image/svg+xml,<svg xmlns=%22http://www.w3.org/2000/svg%22 width=%22400%22 height=%22400%22><rect fill=%22%23667eea%22 width=%22400%22 height=%22400%22/></svg>'">
                </div>
                <div class="card-body">
                    <h6 class="card-title">${escapeHtml(song.title)}</h6>
                    <p class="card-text text-muted">${escapeHtml(song.artist)}</p>
                    <p class="card-text"><small>${escapeHtml(song.album)} • ${escapeHtml(song.genre)}</small></p>
                    <span class="likes-badge">${song.likes} ❤</span>
                </div>
            </div>
        `;
        container.appendChild(card);
    });
}

function renderPagination() {
    const pagination = document.getElementById('pagination');
    pagination.innerHTML = '';
    
    const prevLi = document.createElement('li');
    prevLi.classList.add('page-item');
    if (currentPage === 1) prevLi.classList.add('disabled');
    prevLi.innerHTML = `<a class="page-link" href="#">«</a>`;
    prevLi.addEventListener('click', (e) => {
        e.preventDefault();
        if (currentPage > 1) {
            currentPage--;
            loadSongs();
        }
    });
    pagination.appendChild(prevLi);
    
    const maxPages = 10;
    const startPage = Math.max(1, currentPage - 2);
    const endPage = Math.min(maxPages, currentPage + 2);
    
    for (let i = startPage; i <= endPage; i++) {
        const li = document.createElement('li');
        li.classList.add('page-item');
        if (i === currentPage) li.classList.add('active');
        li.innerHTML = `<a class="page-link" href="#">${i}</a>`;
        li.addEventListener('click', (e) => {
            e.preventDefault();
            currentPage = i;
            loadSongs();
        });
        pagination.appendChild(li);
    }
    
    const nextLi = document.createElement('li');
    nextLi.classList.add('page-item');
    if (currentPage >= maxPages) nextLi.classList.add('disabled');
    nextLi.innerHTML = `<a class="page-link" href="#">»</a>`;
    nextLi.addEventListener('click', (e) => {
        e.preventDefault();
        if (currentPage < maxPages) {
            currentPage++;
            loadSongs();
        }
    });
    pagination.appendChild(nextLi);
}

function escapeHtml(text) {
    const map = {
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#039;'
    };
    return text.replace(/[&<>"']/g, m => map[m]);
}
